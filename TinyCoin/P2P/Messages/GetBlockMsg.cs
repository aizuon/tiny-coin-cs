using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;

namespace TinyCoin.P2P.Messages;

public class GetBlockMsg : IMsg<GetBlockMsg>
{
    private const uint ChunkSize = 50;
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(BlockInfoMsg));
    public string FromBlockId;

    public GetBlockMsg()
    {
        FromBlockId = string.Empty;
    }

    public GetBlockMsg(string fromBlockId)
    {
        FromBlockId = fromBlockId;
    }

    public void Handle(Connection con)
    {
        Logger.Debug("Received GetBlockMsg from {}", con.Channel.RemoteAddress.ToString());

        (var block, long height) = Chain.LocateBlockInActiveChain(FromBlockId);
        if (height == -1)
            height = 1;

        var blocks = new List<Block>((int)ChunkSize);

        uint max_height = (uint)(height + ChunkSize);

        lock (Chain.Mutex)
        {
            if (max_height > Chain.ActiveChain.Count)
                max_height = (uint)Chain.ActiveChain.Count;
            for (uint i = (uint)height; i < max_height; i++)
                blocks.Add(Chain.ActiveChain[(int)i]);
        }

        Logger.Debug("Sending {} block(s) to {}", blocks.Count, con.Channel.RemoteAddress.ToString());
        NetClient.SendMsg(con, new InvMsg(blocks));
    }

    public OpCode GetOpCode()
    {
        return OpCode.GetBlockMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(FromBlockId);

        return buffer;
    }

    public static GetBlockMsg Deserialize(BinaryBuffer buffer)
    {
        var getBlockMsg = new GetBlockMsg();

        if (!buffer.Read(ref getBlockMsg.FromBlockId))
            return null;

        return getBlockMsg;
    }
}
