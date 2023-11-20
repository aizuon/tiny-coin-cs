using System.Collections.Generic;
using TinyCoin.BlockChain;

namespace TinyCoin.P2P.Messages;

public class SendActiveChainMsg : IMsg<SendActiveChainMsg>
{
    public IList<Block> ActiveChain;

    public SendActiveChainMsg()
    {
        ActiveChain = new List<Block>();
    }

    public SendActiveChainMsg(IList<Block> activeChain)
    {
        ActiveChain = activeChain;
    }


    public void Handle(Connection con)
    {
        MsgCache.SendActiveChainMsg = this.Clone();
    }

    public OpCode GetOpCode()
    {
        return OpCode.SendActiveChainMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        lock (Chain.Mutex)
        {
            buffer.WriteSize((uint)Chain.ActiveChain.Count);
            foreach (var block in Chain.ActiveChain)
                buffer.WriteRaw(block.Serialize().Buffer);
        }

        return buffer;
    }

    public static SendActiveChainMsg Deserialize(BinaryBuffer buffer)
    {
        var sendActiveChainMsg = new SendActiveChainMsg();

        uint activeChainSize = 0;
        if (!buffer.ReadSize(ref activeChainSize))
            return null;
        sendActiveChainMsg.ActiveChain = new List<Block>((int)activeChainSize);
        for (uint i = 0; i < activeChainSize; i++)
        {
            var block = Block.Deserialize(buffer);
            if (block == null)
                return null;
            sendActiveChainMsg.ActiveChain.Add(block);
        }

        return sendActiveChainMsg;
    }
}
