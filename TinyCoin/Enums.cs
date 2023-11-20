namespace TinyCoin;

public enum TxStatus : byte
{
    Mempool,
    Mined,
    NotFound
}

public enum NodeType : byte
{
    Unspecified = 0,
    Miner = 1,
    Wallet = 2,
    Full = Miner | Wallet
}
