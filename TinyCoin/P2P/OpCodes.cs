namespace TinyCoin.P2P;

public enum OpCode : byte
{
    BlockInfoMsg,
    GetActiveChainMsg,
    GetBlockMsg,
    GetMemPoolMsg,
    GetUTXOsMsg,
    InvMsg,
    PeerAddMsg,
    PeerHelloMsg,
    SendActiveChainMsg,
    SendMemPoolMsg,
    SendUTXOsMsg,
    TxInfoMsg
}
