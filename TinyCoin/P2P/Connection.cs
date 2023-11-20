using DotNetty.Transport.Channels;

namespace TinyCoin.P2P;

public class Connection
{
    public IChannel Channel;
    public NodeType NodeType = NodeType.Unspecified;

    public Connection(IChannel channel)
    {
        Channel = channel;
    }
}
