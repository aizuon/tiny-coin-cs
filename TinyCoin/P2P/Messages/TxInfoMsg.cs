using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;
using TinyCoin.Txs;

namespace TinyCoin.P2P.Messages;

public class TxInfoMsg : IMsg<TxInfoMsg>
{
    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(TxInfoMsg));

    public Tx Tx;

    public TxInfoMsg()
    {
    }

    public TxInfoMsg(Tx tx)
    {
        Tx = tx;
    }

    public void Handle(Connection con)
    {
        Logger.Debug("Received transaction {TransactionId} from peer {Address}", Tx.Id(),
            con.Channel.RemoteAddress.ToString());

        MemPool.AddTxToMemPool(Tx);
    }

    public static OpCode GetOpCode()
    {
        return OpCode.TxInfoMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteRaw(Tx.Serialize().Buffer);

        return buffer;
    }

    public static TxInfoMsg Deserialize(BinaryBuffer buffer)
    {
        var txInfoMsg = new TxInfoMsg();

        txInfoMsg.Tx = Tx.Deserialize(buffer);
        if (txInfoMsg.Tx == null)
            return null;

        return txInfoMsg;
    }
}
