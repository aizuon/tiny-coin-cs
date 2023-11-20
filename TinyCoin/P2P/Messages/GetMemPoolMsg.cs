namespace TinyCoin.P2P.Messages;

public class GetMemPoolMsg : IMsg<GetMemPoolMsg>
{
    public void Handle(Connection con)
    {
        NetClient.SendMsg(con, new SendMemPoolMsg());
    }

    public OpCode GetOpCode()
    {
        return OpCode.GetMemPoolMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        return buffer;
    }

    public static GetMemPoolMsg Deserialize(BinaryBuffer buffer)
    {
        var getMemPoolMsg = new GetMemPoolMsg();

        return getMemPoolMsg;
    }
}
