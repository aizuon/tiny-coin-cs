namespace TinyCoin.P2P.Messages;

public class PeerAddMsg : IMsg<PeerAddMsg>
{
    public string HostName;
    public ushort Port;

    public PeerAddMsg()
    {
        HostName = string.Empty;
    }

    public PeerAddMsg(string hostname, ushort port)
    {
        HostName = hostname;
        Port = port;
    }

    public void Handle(Connection con)
    {
        NetClient.Connect(HostName, Port);
    }

    public static OpCode GetOpCode()
    {
        return OpCode.PeerAddMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(HostName);
        buffer.Write(Port);

        return buffer;
    }

    public static PeerAddMsg Deserialize(BinaryBuffer buffer)
    {
        var peerAddMsg = new PeerAddMsg();

        if (!buffer.Read(ref peerAddMsg.HostName))
            return null;
        if (!buffer.Read(ref peerAddMsg.Port))
            return null;

        return peerAddMsg;
    }
}
