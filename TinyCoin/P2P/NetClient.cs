using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;
using Serilog.Core;
using TinyCoin.P2P.Messages;

namespace TinyCoin.P2P;

public static class NetClient
{
    private static readonly byte[] Magic = { 0xf9, 0xbe, 0xb4, 0xd9 };

    public static readonly IList<(string, ushort)> InitialPeers = new List<(string, ushort)>
        { ("127.0.0.1", 9900), ("127.0.0.1", 9901), ("127.0.0.1", 9902), ("127.0.0.1", 9903), ("127.0.0.1", 9904) };

    public static object ConnectionsMutex = new object();
    public static IList<Connection> Connections = new List<Connection>();
    public static IList<Connection> MinerConnections = new List<Connection>();
    private static readonly MultithreadEventLoopGroup Group = new MultithreadEventLoopGroup(2);

    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(NetClient));

    private static readonly Dictionary<OpCode, Action<BinaryBuffer, IChannelHandlerContext>> MessageHandlers =
        new Dictionary<OpCode, Action<BinaryBuffer, IChannelHandlerContext>>();

    public static void Init()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMsg<>)) &&
                        t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in types)
        {
            var method = typeof(NetClient)
                .GetMethod(nameof(HandleMessage), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
            var opcode = (OpCode)type
                .GetMethod(nameof(IHandleable.GetOpCode), BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            MessageHandlers[opcode] =
                (Action<BinaryBuffer, IChannelHandlerContext>)Delegate.CreateDelegate(
                    typeof(Action<BinaryBuffer, IChannelHandlerContext>), method);
        }
    }

    public static void Stop()
    {
        lock (ConnectionsMutex)
        {
            foreach (var con in Connections)
                con.Channel.CloseAsync().Wait();

            Connections.Clear();
            MinerConnections.Clear();
        }

        Group.ShutdownGracefullyAsync().Wait();
    }

    public static void Connect(string address, int port)
    {
        try
        {
            var bootstrap = new Bootstrap();
            bootstrap
                .Group(Group)
                .Channel<TcpSocketChannel>()
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;
                    pipeline.AddLast(new PacketDecoder());
                    pipeline.AddLast(new PacketEncoder());
                    pipeline.AddLast(new NetClientHandler());
                }));

            var con = new Connection(bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(address), port)).GetAwaiter()
                .GetResult());

            lock (Connections)
            {
                Connections.Add(con);
            }
        }
        catch (ConnectException ex)
        {
            // Logger.Error(ex, "Unable to connect to {Address}:{Port}", address, port);
        }
    }

    public static void ListenAsync(int port)
    {
        var bootstrap = new ServerBootstrap();
        bootstrap
            .Group(Group)
            .Channel<TcpServerSocketChannel>()
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                var pipeline = channel.Pipeline;
                pipeline.AddLast(new PacketDecoder());
                pipeline.AddLast(new PacketEncoder());
                pipeline.AddLast(new NetClientHandler());
            }));

        bootstrap.BindAsync(port).Wait();
    }

    public static void SendMsg<T>(Connection con, T msg) where T : IMsg<T>, new()
    {
        var buffer = PrepareSendBuffer(msg);
        con.Channel.WriteAndFlushAsync(buffer).Wait();
    }

    public static bool SendMsgRandom<T>(T msg) where T : IMsg<T>, new()
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

    public static void BroadcastMsg<T>(T msg) where T : IMsg<T>, new()
    {
        var buffer = PrepareSendBuffer(msg);

        lock (Connections)
        {
            foreach (var con in MinerConnections)
                con.Channel.WriteAndFlushAsync(buffer).Wait();
        }
    }

    private static BinaryBuffer PrepareSendBuffer<T>(T msg) where T : IMsg<T>, new()
    {
        byte[] serializedMsg = msg.Serialize().Buffer;
        var opcode = T.GetOpCode();

        var buffer = new BinaryBuffer();
        buffer.Write(opcode);
        buffer.WriteRaw(serializedMsg);

        return buffer;
    }

    private static void HandleMessage<T>(BinaryBuffer buffer, IChannelHandlerContext ctx) where T : IMsg<T>, new()
    {
        var msg = T.Deserialize(buffer);
        if (msg == null)
        {
            Logger.Error("Unable to deserialize opcode {OpCode}", T.GetOpCode());
            return;
        }

        Connection con;
        lock (ConnectionsMutex)
        {
            con = Connections.FirstOrDefault(con => con.Channel == ctx.Channel);
        }

        msg.Handle(con);
    }

    public class PacketDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            while (input.IsReadable())
            {
                input.MarkReaderIndex();

                int magicIndex = IndexOf(input, Magic);
                if (magicIndex < 0)
                {
                    input.ResetReaderIndex();
                    return;
                }

                byte[] data = new byte[magicIndex - input.ReaderIndex];
                input.ReadBytes(data);

                input.SkipBytes(Magic.Length);

                output.Add(new BinaryBuffer(data));
            }
        }

        private static int IndexOf(IByteBuffer haystack, byte[] needle)
        {
            for (int i = haystack.ReaderIndex; i < haystack.WriterIndex; i++)
            {
                int haystackIndex = i;
                int needleIndex;

                for (needleIndex = 0; needleIndex < needle.Length; needleIndex++)
                {
                    if (haystack.GetByte(haystackIndex) != needle[needleIndex])
                        break;
                    haystackIndex++;
                    if (haystackIndex == haystack.WriterIndex && needleIndex != needle.Length - 1)
                        return -1;
                }

                if (needleIndex == needle.Length)
                    return i;
            }

            return -1;
        }
    }

    public class PacketEncoder : MessageToByteEncoder<BinaryBuffer>
    {
        protected override void Encode(IChannelHandlerContext context, BinaryBuffer message, IByteBuffer output)
        {
            if (message == null)
                return;

            output.WriteBytes(message.Buffer);
            output.WriteBytes(Magic);
        }
    }

    public class NetClientHandler : ChannelHandlerAdapter
    {
        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            var con = new Connection(ctx.Channel);
            lock (ConnectionsMutex)
            {
                Connections.Add(con);
            }

            SendMsg(con, new PeerHelloMsg());
        }

        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (message is BinaryBuffer buffer)
            {
                var opCode = OpCode.PeerHelloMsg;
                if (!buffer.Read(ref opCode))
                    return;
                if (MessageHandlers.TryGetValue(opCode, out var handler))
                    handler(buffer, ctx);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception ex)
        {
            Logger.Error(ex, "Error while handling message from {RemoteAddress}", ctx.Channel.RemoteAddress);

            ctx.CloseAsync().Wait();
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            lock (ConnectionsMutex)
            {
                var con = Connections.FirstOrDefault(con => con.Channel == ctx.Channel);
                Connections.Remove(con);
            }
        }
    }
}
