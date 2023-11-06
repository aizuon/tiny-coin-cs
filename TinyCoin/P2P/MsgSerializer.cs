using System.Collections.Generic;
using TinyCoin.Crypto;
using TinyCoin.Txs;

namespace TinyCoin.P2P;

public static class MsgSerializer
{
    public static byte[] BuildSpendMsg(TxOutPoint toSpend, byte[] pubKey, int sequence, IList<TxOut> txOuts)
    {
        var spendMessage = new BinaryBuffer();
        spendMessage.WriteRaw(toSpend.Serialize().Buffer);
        spendMessage.Write(sequence);
        spendMessage.Write(pubKey);
        foreach (var txOut in txOuts)
            spendMessage.WriteRaw(txOut.Serialize().Buffer);

        byte[] buffer = spendMessage.Buffer;

        byte[] hash = SHA256.DoubleHashBinary(buffer);

        return hash;
    }
}
