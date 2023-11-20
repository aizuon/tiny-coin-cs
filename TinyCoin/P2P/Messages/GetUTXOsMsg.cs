namespace TinyCoin.P2P.Messages;

public class GetUTXOsMsg : IMsg<GetUTXOsMsg>
{
    public void Handle(Connection con)
    {
        NetClient.SendMsg(con, new SendUTXOsMsg());
    }

    public static OpCode GetOpCode()
    {
        return OpCode.GetUTXOsMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        return buffer;
    }

    public static GetUTXOsMsg Deserialize(BinaryBuffer buffer)
    {
        var getUTXOsMsg = new GetUTXOsMsg();

        return getUTXOsMsg;
    }
}
