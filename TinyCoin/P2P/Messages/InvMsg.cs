using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;

namespace TinyCoin.P2P.Messages;

public class InvMsg : IMsg<InvMsg>
{
    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(BlockInfoMsg));

    public IList<Block> Blocks;

    public InvMsg()
    {
        Blocks = new List<Block>();
    }

    public InvMsg(IList<Block> blocks)
    {
        Blocks = blocks;
    }

    public void Handle(Connection con)
    {
        Logger.Information("Received initial sync from {RemoteEndPoint}", con.Channel.RemoteAddress.ToString());

        var newBlocks = new List<Block>();
        foreach (var block in Blocks)
        {
            (var foundBlock, long foundHeight, long foundIdx) = Chain.LocateBlockInAllChains(block.Id());
            if (foundBlock == null)
                newBlocks.Add(block);
        }

        if (newBlocks.Count == 0)
        {
            Logger.Information("Initial block download complete");

            Chain.InitialBlockDownloadComplete.Value = true;
            Chain.SaveToDisk();

            return;
        }

        foreach (var newBlock in newBlocks)
            Chain.ConnectBlock(newBlock);

        string newTipId = Chain.ActiveChain.Last().Id();
        Logger.Information("Continuing initial sync from {BlockId}", newTipId);

        NetClient.SendMsg(con, new GetBlockMsg(newTipId));
    }

    public static OpCode GetOpCode()
    {
        return OpCode.InvMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteSize((uint)Blocks.Count);
        foreach (var block in Blocks)
            buffer.WriteRaw(block.Serialize().Buffer);

        return buffer;
    }

    public static InvMsg Deserialize(BinaryBuffer buffer)
    {
        var invMsg = new InvMsg();

        uint blocksSize = 0;
        if (!buffer.ReadSize(ref blocksSize))
            return null;

        invMsg.Blocks = new List<Block>((int)blocksSize);
        for (uint i = 0; i < blocksSize; i++)
        {
            var block = Block.Deserialize(buffer);
            if (block == null)
                return null;
            invMsg.Blocks.Add(block);
        }

        return invMsg;
    }
}
