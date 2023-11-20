using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;
using Serilog.Core;
using TinyCoin.P2P.Messages;

namespace TinyCoin.P2P;

public static class NetClient
{
    private const string Magic = "\xf9\xbe\xb4\xd9";

    public static readonly IList<(string, ushort)> InitialPeers = new List<(string, ushort)>
        { ("127.0.0.1", 9900), ("127.0.0.1", 9901), ("127.0.0.1", 9902), ("127.0.0.1", 9903), ("127.0.0.1", 9904) };

    public static object ConnectionsMutex = new object();
    public static IList<Connection> Connections = new List<Connection>();
    public static IList<Connection> MinerConnections = new List<Connection>();
    private static readonly MultithreadEventLoopGroup Group = new MultithreadEventLoopGroup(2);
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(NetClient));

    public static void Stop()
    {
        lock (ConnectionsMutex)
        {
            foreach (var con in Connections)
                con.Channel.CloseAsync();

            Connections.Clear();
            MinerConnections.Clear();
        }

        Group.ShutdownGracefullyAsync();
    }

    public static void Connect(string address, int port)
    {
        var bootstrap = new Bootstrap();
        bootstrap
            .Group(Group)
            .Channel<TcpSocketChannel>()
            .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new NetClientHandler());
            }));

        var con = new Connection(bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(address), port)).GetAwaiter()
            .GetResult());

        lock (Connections)
        {
            Connections.Add(con);
        }

        SendMsg(con, new PeerHelloMsg());
    }

    public static void ListenAsync(int port)
    {
        var bootstrap = new ServerBootstrap();
        bootstrap
            .Group(Group)
            .Channel<TcpServerSocketChannel>()
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new NetClientHandler());
            }));

        bootstrap.BindAsync(port).Wait();
    }

    public static void SendMsg<T>(Connection con, IMsg<T> msg)
    {
        var buffer = PrepareSendBuffer(msg);
        con.Channel.WriteAndFlushAsync(buffer);
    }

    public static bool SendMsgRandom<T>(IMsg<T> msg)
    {
        Connection con;

        lock (Connections)
        {
            if (!MinerConnections.Any())
                return false;

            var random = new Random();
            int index = random.Next(MinerConnections.Count);
            con = MinerConnections[index];
        }

        SendMsg(con, msg);

        return true;
    }

    public static void BroadcastMsg<T>(IMsg<T> msg)
    {
        var buffer = PrepareSendBuffer(msg);

        lock (Connections)
        {
            foreach (var con in MinerConnections)
                con.Channel.WriteAndFlushAsync(buffer);
        }
    }

    private static IByteBuffer PrepareSendBuffer<T>(IMsg<T> msg)
    {
        var buffer = Unpooled.Buffer();

        byte[] serializedMsg = msg.Serialize().Buffer;
        var opcode = msg.GetOpCode();

        var msgBuffer = new BinaryBuffer(serializedMsg);
        msgBuffer.Write(opcode);
        msgBuffer.WriteRaw(serializedMsg);
        msgBuffer.WriteRaw(Magic);

        buffer.WriteBytes(msgBuffer.Buffer);
        return buffer;
    }
}

public class NetClientHandler : ChannelHandlerAdapter
{
    private static readonly ILogger Logger =
        Log.ForContext(Constants.SourceContextPropertyName, nameof(NetClientHandler));

    public override void ChannelActive(IChannelHandlerContext ctx)
    {
        base.ChannelActive(ctx);

        var con = new Connection(ctx.Channel);
        lock (NetClient.ConnectionsMutex)
        {
            NetClient.Connections.Add(con);
        }

        NetClient.SendMsg(con, new PeerHelloMsg());
    }

    public override void ChannelRead(IChannelHandlerContext ctx, object message)
    {
        base.ChannelRead(ctx, message);

        if (message is IByteBuffer buffer)
        {
            byte[] arr = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(arr);
            var binaryBuffer = new BinaryBuffer(arr);
            var opCode = OpCode.PeerHelloMsg;
            if (!binaryBuffer.Read(ref opCode))
                return;
            dynamic msgType = null;
            switch (opCode)
            {
                case OpCode.BlockInfoMsg:
                {
                    msgType = typeof(BlockInfoMsg);

                    break;
                }
                case OpCode.GetActiveChainMsg:
                {
                    msgType = typeof(GetActiveChainMsg);

                    break;
                }
                case OpCode.GetBlockMsg:
                {
                    msgType = typeof(GetBlockMsg);

                    break;
                }
                case OpCode.GetMemPoolMsg:
                {
                    msgType = typeof(GetMemPoolMsg);

                    break;
                }
                case OpCode.GetUTXOsMsg:
                {
                    msgType = typeof(GetUTXOsMsg);

                    break;
                }
                case OpCode.InvMsg:
                {
                    msgType = typeof(InvMsg);

                    break;
                }
                case OpCode.PeerAddMsg:
                {
                    msgType = typeof(PeerAddMsg);

                    break;
                }
                case OpCode.PeerHelloMsg:
                {
                    msgType = typeof(PeerHelloMsg);

                    break;
                }
                case OpCode.SendActiveChainMsg:
                {
                    msgType = typeof(SendActiveChainMsg);

                    break;
                }
                case OpCode.SendMemPoolMsg:
                {
                    msgType = typeof(SendMemPoolMsg);

                    break;
                }
                case OpCode.SendUTXOsMsg:
                {
                    msgType = typeof(SendUTXOsMsg);

                    break;
                }
                case OpCode.TxInfoMsg:
                {
                    msgType = typeof(TxInfoMsg);

                    break;
                }
            }

            dynamic msg = msgType.Deserialize(binaryBuffer);
            if (msg == null)
            {
                Logger.Error("Unable to deserialize opcode {}", opCode);

                return;
            }

            var con = NetClient.Connections.FirstOrDefault(con => con.Channel == ctx.Channel);
            msg.Handle(con);
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
    {
        base.ExceptionCaught(ctx, exception);

        ctx.CloseAsync();
    }
}
