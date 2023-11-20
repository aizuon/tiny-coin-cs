using TinyCoin.P2P.Messages;

namespace TinyCoin.P2P;

public class MsgCache
{
    public const ushort MaxMsgAwaitTimeInSecs = 60;

    public static SendActiveChainMsg SendActiveChainMsg;
    public static SendMemPoolMsg SendMemPoolMsg;
    public static SendUTXOsMsg SendUTXOsMsg;
}
