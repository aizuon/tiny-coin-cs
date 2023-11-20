using System.Collections.Generic;

namespace TinyCoin.P2P.Messages;

public class SendMemPoolMsg : IMsg<SendMemPoolMsg>
{
    public IList<string> MemPool;

    public SendMemPoolMsg()
    {
        MemPool = new List<string>();
    }

    public SendMemPoolMsg(IList<string> memPool)
    {
        MemPool = memPool;
    }

    public void Handle(Connection con)
    {
        MsgCache.SendMemPoolMsg = this.Clone();
    }

    public OpCode GetOpCode()
    {
        return OpCode.SendMemPoolMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        lock (BlockChain.MemPool.Mutex)
        {
            buffer.WriteSize((uint)BlockChain.MemPool.Map.Count);
            foreach (string key in BlockChain.MemPool.Map.Keys)
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
