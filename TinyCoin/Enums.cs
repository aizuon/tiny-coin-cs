using System;

namespace TinyCoin;

public enum TxStatus : byte
{
    MemPool,
    Mined,
    NotFound
}

[Flags]
public enum NodeType : byte
{
    Unspecified = 0,
    Miner = 1 << 0,
    Wallet = 1 << 1,
    Full = Miner | Wallet
}
