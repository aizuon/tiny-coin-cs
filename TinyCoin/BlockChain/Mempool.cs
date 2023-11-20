using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using TinyCoin.P2P;
using TinyCoin.P2P.Messages;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.BlockChain;

public static class MemPool
{
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(MemPool));
    public static Dictionary<string, Tx> Map = new Dictionary<string, Tx>();
    public static List<Tx> OrphanedTxs = new List<Tx>();
    public static readonly object Mutex = new object();

    public static UTXO Find_UTXO_InMemPool(TxOutPoint txOutPoint)
    {
        lock (Mutex)
        {
            if (!Map.ContainsKey(txOutPoint.TxId))
                return null;

            var tx = Map[txOutPoint.TxId];
            if (tx.TxOuts.Count - 1 < txOutPoint.TxOutIdx)
            {
                Logger.Error("Unable to find UTXO in MemPool for {}", txOutPoint.TxId);
                return null;
            }

            var txOut = tx.TxOuts[(int)txOutPoint.TxOutIdx];
            return new UTXO(txOut, txOutPoint, false, -1);
        }
    }

    public static Block SelectFromMemPool(Block block)
    {
        lock (Mutex)
        {
            var newBlock = block;

            var mapVector = Map.ToList();
            mapVector.Sort((a, b) => PoW.CalculateFees(a.Value).CompareTo(PoW.CalculateFees(b.Value)));

            var addedToBlock = new HashSet<string>();
            foreach ((string txId, var _) in mapVector)
                newBlock = TryAddToBlock(newBlock, txId, addedToBlock);

            return newBlock;
        }
    }

    public static void AddTxToMemPool(Tx tx)
    {
        lock (Mutex)
        {
            string txId = tx.Id();
            if (Map.ContainsKey(txId))
            {
                Logger.Information("Transaction {} already seen", txId);
                return;
            }

            try
            {
                tx.Validate(new Tx.ValidateRequest());
            }
            catch (TxValidationException ex)
            {
                Logger.Error(ex, "Transaction validation failed for {}", txId);

                if (ex.ToOrphan != null)
                {
                    Logger.Information("Transaction {} submitted as orphan", ex.ToOrphan.Id());
                    OrphanedTxs.Add(ex.ToOrphan);
                    return;
                }

                Logger.Error("Transaction {} rejected", txId);
                return;
            }

            Map[txId] = tx;
            Logger.Debug("Transaction {} added to MemPool", txId);

            NetClient.SendMsgRandom(new TxInfoMsg(tx));
        }
    }

    private static bool CheckBlockSize(Block block)
    {
        return block.Serialize().Buffer.Length < NetParams.MaxBlockSerializedSizeInBytes;
    }

    private static Block TryAddToBlock(Block block, string txId, ISet<string> addedToBlock)
    {
        lock (Mutex)
        {
            if (addedToBlock.Contains(txId))
                return block;

            if (!Map.ContainsKey(txId))
                return block;

            var tx = Map[txId];
            foreach (var txIn in tx.TxIns)
            {
                var toSpend = txIn.ToSpend;
                if (UTXO.FindInMap(toSpend) != null)
                    continue;

                var inMemPool = Find_UTXO_InMemPool(toSpend);
                if (inMemPool == null)
                {
                    Logger.Error("Unable to find UTXO for {}", txIn.ToSpend.TxId);
                    return null;
                }

                block = TryAddToBlock(block, inMemPool.TxOutPoint.TxId, addedToBlock);
                if (block == null)
                {
                    Logger.Error("Unable to add parent");
                    return null;
                }
            }

            var newBlock = block;
            var blockTxs = block.Txs;
            var txs = new List<Tx>(blockTxs) { tx };
            newBlock.Txs = txs;

            if (CheckBlockSize(newBlock))
            {
                Logger.Debug("Added transaction {} to block {}", txId, block.Id());
                addedToBlock.Add(txId);
                return newBlock;
            }

            return block;
        }
    }
}
