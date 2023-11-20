using DotNetty.Transport.Channels;

namespace TinyCoin.P2P;

public class Connection
{
    public IChannel Channel;
    public NodeType NodeType = NodeType.Unspecified;
    public object WriteMutex = new object();

    public Connection(IChannel channel)
    {
        Channel = channel;
    }
}
