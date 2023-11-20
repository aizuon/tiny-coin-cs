namespace TinyCoin.P2P.Messages;

public class GetActiveChainMsg : IMsg<GetActiveChainMsg>
{
    public void Handle(Connection con)
    {
        NetClient.SendMsg(con, new SendActiveChainMsg());
    }

    public OpCode GetOpCode()
    {
        return OpCode.GetActiveChainMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        return buffer;
    }

    public static GetActiveChainMsg Deserialize(BinaryBuffer buffer)
    {
        var getActiveChainMsg = new GetActiveChainMsg();

        return getActiveChainMsg;
    }
}
