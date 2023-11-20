using System.Linq;

namespace TinyCoin.P2P.Messages;

public class PeerHelloMsg : IMsg<PeerHelloMsg>
{
    public NodeType NodeType = NodeType.Unspecified;

    public PeerHelloMsg()
    {
        NodeType = NodeConfig.NodeType;
    }

    public PeerHelloMsg(NodeType type)
    {
        NodeType = type;
    }

    public void Handle(Connection con)
    {
        con.NodeType = NodeType;
        if (con.NodeType.HasFlag(NodeType.Miner))
            lock (NetClient.ConnectionsMutex)
            {
                var connection = NetClient.MinerConnections.FirstOrDefault(o => o == con);

                if (connection == null)
                    NetClient.MinerConnections.Add(con);
            }
    }

    public static OpCode GetOpCode()
    {
        return OpCode.PeerHelloMsg;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(NodeType);

        return buffer;
    }

    public static PeerHelloMsg Deserialize(BinaryBuffer buffer)
    {
        var peerHelloMsg = new PeerHelloMsg();

        if (!buffer.Read(ref peerHelloMsg.NodeType))
            return null;

        return peerHelloMsg;
    }
}
