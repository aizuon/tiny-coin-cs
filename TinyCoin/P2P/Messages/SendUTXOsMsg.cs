using System.Collections.Generic;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.P2P.Messages;

public class SendUTXOsMsg : IMsg<SendUTXOsMsg>
{
    public IDictionary<TxOutPoint, UTXO> UTXOs;

    public SendUTXOsMsg()
    {
        UTXOs = new Dictionary<TxOutPoint, UTXO>();
    }

    public SendUTXOsMsg(IDictionary<TxOutPoint, UTXO> utxos)
    {
        UTXOs = utxos;
    }

    public void Handle(Connection con)
    {
        MsgCache.SendUTXOsMsg = this.Clone();
    }

    public static OpCode GetOpCode()
    {
        return OpCode.SendUTXOsMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        lock (UTXO.Mutex)
        {
            buffer.WriteSize((uint)UTXO.Map.Count);
            foreach (var (key, value) in UTXO.Map)
            {
                buffer.WriteRaw(key.Serialize().Buffer);
                buffer.WriteRaw(value.Serialize().Buffer);
            }
        }

        return buffer;
    }

    public static SendUTXOsMsg Deserialize(BinaryBuffer buffer)
    {
        var sendUTXOsMsg = new SendUTXOsMsg();

        uint utxoMapSize = 0;
        if (!buffer.ReadSize(ref utxoMapSize))
            return null;
        sendUTXOsMsg.UTXOs = new Dictionary<TxOutPoint, UTXO>((int)utxoMapSize);
        for (uint i = 0; i < utxoMapSize; i++)
        {
            var txOutPoint = TxOutPoint.Deserialize(buffer);
            if (txOutPoint == null)
                return null;
            var utxo = UTXO.Deserialize(buffer);
            if (utxo == null)
                return null;
            sendUTXOsMsg.UTXOs[txOutPoint] = utxo;
        }

        return sendUTXOsMsg;
    }
}
