using System.Collections.Generic;
using System.Linq;

namespace TinyCoin.P2P.Messages;

public class SendMemPoolMsg : IMsg<SendMemPoolMsg>
{
    public IList<string> MemPool;

    public SendMemPoolMsg()
    {
        MemPool = BlockChain.MemPool.Map.Keys.ToList();
    }

    public SendMemPoolMsg(IList<string> memPool)
    {
        MemPool = memPool;
    }

    public void Handle(Connection con)
    {
        MsgCache.SendMemPoolMsg = this.Clone();
    }

    public static OpCode GetOpCode()
    {
        return OpCode.SendMemPoolMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        lock (BlockChain.MemPool.Mutex)
        {
            buffer.WriteSize((uint)MemPool.Count);
            foreach (string key in MemPool)
                buffer.Write(key);
        }

        return buffer;
    }

    public static SendMemPoolMsg Deserialize(BinaryBuffer buffer)
    {
        var sendMemPoolMsg = new SendMemPoolMsg();

        uint mempoolSize = 0;
        if (!buffer.ReadSize(ref mempoolSize))
            return null;
        sendMemPoolMsg.MemPool = new List<string>((int)mempoolSize);
        for (uint i = 0; i < mempoolSize; i++)
        {
            string tx = string.Empty;
            if (!buffer.Read(ref tx))
                return null;
            sendMemPoolMsg.MemPool.Add(tx);
        }

        return sendMemPoolMsg;
    }
}
