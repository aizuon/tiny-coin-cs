using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;

namespace TinyCoin.P2P.Messages;

public class BlockInfoMsg : IMsg<BlockInfoMsg>
{
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(BlockInfoMsg));
    public Block Block;

    public BlockInfoMsg()
    {
    }

    public BlockInfoMsg(Block block)
    {
        Block = block;
    }

    public void Handle(Connection con)
    {
        Logger.Debug("Received block {} from peer {}", Block.Id(), con.Channel.RemoteAddress);

        Chain.ConnectBlock(Block);
        Chain.SaveToDisk();
    }

    public OpCode GetOpCode()
    {
        return OpCode.BlockInfoMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteRaw(Block.Serialize().Buffer);

        return buffer;
    }

    public static BlockInfoMsg Deserialize(BinaryBuffer buffer)
    {
        var blockInfoMsg = new BlockInfoMsg();

        blockInfoMsg.Block = Block.Deserialize(buffer);
        if (blockInfoMsg.Block == null)
            return null;

        return blockInfoMsg;
    }
}
