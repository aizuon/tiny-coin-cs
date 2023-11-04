using System;
using TinyCoin.Txs;

namespace TinyCoin;

public class TxUnlockException : Exception
{
    public TxUnlockException(string msg) : base(msg)
    {
    }
}

public class TxValidationException : Exception
{
    public Tx ToOrphan;

    public TxValidationException(string msg) : base(msg)
    {
    }

    public TxValidationException(string msg, Tx toOrphan) : base(msg)
    {
        ToOrphan = toOrphan;
    }
}

// public class BlockValidationException : Exception
// {
//     public Block ToOrphan;
//
//     public BlockValidationException(string msg) : base(msg)
//     {
//     }
//
//     public BlockValidationException(string msg, Block toOrphan) : base(msg)
//     {
//         ToOrphan = toOrphan;
//     }
// }
