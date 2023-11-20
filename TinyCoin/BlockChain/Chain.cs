using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Serilog;
using Serilog.Core;
using TinyCoin.Crypto;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.BlockChain;

public static class Chain
{
    private const string ChainPath = "chain.dat";
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(Chain));

    public static readonly TxIn GenesisTxIn = new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1);
    public static readonly TxOut GenesisTxOut = new TxOut(5000000000, "143UVyz7ooiAv1pMqbwPPpnH4BV9ifJGFF");
    public static readonly Tx GenesisTx = new Tx(new List<TxIn> { GenesisTxIn }, new List<TxOut> { GenesisTxOut }, 0);

    public static readonly Block GenesisBlock = new Block(0, "",
        "75b7747cdbad68d5e40269399d9d8d6c048cc80a9e1b355379a5ed831ffbc1a8", 1501821412, 24, 13835058055287124368,
        new List<Tx> { GenesisTx });

    public static IList<Block> ActiveChain = new List<Block> { GenesisBlock };
    public static IList<IList<Block>> SideBranches = new List<IList<Block>>();

    public static IList<Block> OrphanBlocks = new List<Block>();

    public static readonly object Mutex = new object();

    public static uint ActiveChainIdx = 0;

    public static AtomicBool InitialBlockDownloadComplete = new AtomicBool(false);

    public static uint GetCurrentHeight()
    {
        lock (Mutex)
        {
            return (uint)ActiveChain.Count;
        }
    }

    public static long GetMedianTimePast(uint numLastBlocks)
    {
        lock (Mutex)
        {
            if (numLastBlocks > ActiveChain.Count)
                return 0;

            uint firstIdx = (uint)ActiveChain.Count - numLastBlocks;
            uint medianIdx = firstIdx + numLastBlocks / 2;
            if (numLastBlocks % 2 == 0)
                medianIdx -= 1;

            return ActiveChain[(int)medianIdx].Timestamp;
        }
    }

    public static uint ValidateBlock(Block block)
    {
        lock (Mutex)
        {
            var txs = block.Txs;

            if (txs.Count == 0)
                throw new BlockValidationException("Transactions empty");

            if (block.Timestamp - Utils.GetUnixTimestamp() >
                NetParams.MaxFutureBlockTimeInSecs)
                throw new BlockValidationException("Block timestamp too far in future");

            var targetHash = BigInteger.One << (byte.MaxValue - block.Bits);
            if (!HashChecker.IsValid(block.Id(), targetHash))
                throw new BlockValidationException("Block header does not satisfy bits");

            var coinbaseTransactions = txs.Select((tx, index) => new { tx, index })
                .Where(x => x.tx.IsCoinbase())
                .ToList();

            if (coinbaseTransactions.Count == 0 || coinbaseTransactions[0].index != 0 || coinbaseTransactions.Count > 1)
                throw new BlockValidationException("First transaction must be coinbase and no more");

            for (uint i = 0; i < txs.Count; i++)
                try
                {
                    txs[(int)i].ValidateBasics(i == 0);
                }
                catch (TxValidationException ex)
                {
                    Logger.Error(ex, "");

                    Logger.Error("Transaction {} in block {} failed validation", txs[(int)i].Id(), block.Id());

                    throw new BlockValidationException($"Transaction {txs[(int)i].Id()} invalid");
                }

            if (MerkleTree.GetRootOfTxs(txs).Value != block.MerkleHash)
                throw new BlockValidationException("Merkle hash invalid");

            if (block.Timestamp <= GetMedianTimePast(11))
                throw new BlockValidationException("Timestamp too old");

            uint prevBlockChainIdx;
            if (block.PrevBlockHash.Length == 0)
            {
                prevBlockChainIdx = ActiveChainIdx;
            }
            else
            {
                (var prevBlock, long _, long prevBlockChainIdx2) =
                    LocateBlockInAllChains(block.PrevBlockHash);
                if (prevBlock == null)
                    throw new BlockValidationException(
                        $"Previous block {block.PrevBlockHash} not found in any chain", block);

                if (prevBlockChainIdx2 != ActiveChainIdx)
                    return (uint)prevBlockChainIdx2;
                if (prevBlock.Id() != ActiveChain.Last().Id())
                    return (uint)prevBlockChainIdx2 + 1;

                prevBlockChainIdx = (uint)prevBlockChainIdx2;
            }

            if (PoW.GetNextWorkRequired(block.PrevBlockHash) != block.Bits)
                throw new BlockValidationException("Bits incorrect");

            var nonCoinbaseTxs = block.Txs.Skip(1).ToList();
            var req = new Tx.ValidateRequest
            {
                SiblingsInBlock = nonCoinbaseTxs,
                Allow_UTXO_FromMempool = false
            };
            foreach (var nonCoinbaseTx in nonCoinbaseTxs)
                try
                {
                    nonCoinbaseTx.Validate(req);
                }
                catch (TxValidationException ex)
                {
                    Logger.Error(ex, "");

                    throw new BlockValidationException($"Transaction {nonCoinbaseTx.Id()} failed to validate");
                }

            return prevBlockChainIdx;
        }
    }

    public static long ConnectBlock(Block block, bool doingReorg = false)
    {
        lock (Mutex)
        {
            string blockId = block.Id();

            Block locatedBlock;
            if (!doingReorg)
            {
                var (locatedBlock2, _, _) =
                    LocateBlockInAllChains(block.Id());
                locatedBlock = locatedBlock2;
            }
            else
            {
                var (locatedBlock2, _) = LocateBlockInActiveChain(block.Id());
                locatedBlock = locatedBlock2;
            }

            if (locatedBlock != null)
            {
                Logger.Information("Ignore already seen block {}", blockId);

                return -1;
            }

            uint chainIdx;
            try
            {
                chainIdx = ValidateBlock(block);
            }
            catch (BlockValidationException ex)
            {
                Logger.Error(ex, "");

                Logger.Error("Block {} failed validation", blockId);
                if (ex.ToOrphan != null)
                {
                    Logger.Information("Found orphan block {}", blockId);

                    OrphanBlocks.Add(ex.ToOrphan);
                }

                return -1;
            }

            if (chainIdx != ActiveChainIdx && SideBranches.Count < chainIdx)
            {
                Logger.Information("Creating a new side branch with idx {} for block {}", chainIdx, blockId);

                SideBranches.Add(new List<Block>());
            }

            Logger.Information("Connecting block {} to chain {}", blockId, chainIdx);

            var chain = chainIdx == ActiveChainIdx ? ActiveChain : SideBranches[(int)(chainIdx - 1)];
            chain.Add(block);

            if (chainIdx == ActiveChainIdx)
                foreach (var tx in block.Txs)
                {
                    string txId = tx.Id();

                    lock (Mempool.Mutex)
                    {
                        Mempool.Map.Remove(txId);
                    }

                    if (!tx.IsCoinbase())
                        foreach (var txIn in tx.TxIns)
                            UTXO.RemoveFromMap(txIn.ToSpend.TxId, txIn.ToSpend.TxOutIdx);

                    for (uint i = 0; i < tx.TxOuts.Count; i++)
                        UTXO.AddToMap(tx.TxOuts[(int)i], txId, i, tx.IsCoinbase(), chain.Count);
                }

            if ((!doingReorg && ReorgIfNecessary()) || chainIdx == ActiveChainIdx)
            {
                PoW.MineInterrupt.Value = true;

                Logger.Information("Block accepted at height {} with {} txs", ActiveChain.Count - 1, block.Txs.Count);
            }

            // NetClient.SendMsgRandom(BlockInfoMsg(block));

            return chainIdx;
        }
    }

    public static Block DisconnectBlock(Block block)
    {
        lock (Mutex)
        {
            string blockId = block.Id();

            var back = ActiveChain.Last();
            if (blockId != back.Id())
                throw new Exception("Block being disconnected must be the tip");

            foreach (var tx in block.Txs)
            {
                string txId = tx.Id();

                lock (Mempool.Mutex)
                {
                    Mempool.Map[txId] = tx;
                }

                foreach (var txIn in tx.TxIns)
                    if (txIn.ToSpend != null)
                    {
                        (var txOut, _, long txOutIdx, bool isCoinbase, long height) =
                            FindTxOutForTxInInActiveChain(txIn);

                        UTXO.AddToMap(txOut, txId, txOutIdx, isCoinbase, height);
                    }

                for (uint i = 0; i < tx.TxOuts.Count; i++)
                    UTXO.RemoveFromMap(txId, i);
            }

            ActiveChain.RemoveAt(ActiveChain.Count - 1);

            Logger.Information("Block {} disconnected", blockId);

            return back;
        }
    }

    public static IList<Block> DisconnectToFork(Block forkBlock)
    {
        lock (Mutex)
        {
            var disconnectedChain = new List<Block>();

            string forkBlockId = forkBlock.Id();
            while (ActiveChain.Last().Id() != forkBlockId)
                disconnectedChain.Add(DisconnectBlock(ActiveChain.Last()));

            disconnectedChain.Reverse();

            return disconnectedChain;
        }
    }

    public static bool ReorgIfNecessary()
    {
        lock (Mutex)
        {
            bool reorged = false;

            var frozenSideBranches = SideBranches.ToList();
            uint branchIdx = 1;
            foreach (var chain in frozenSideBranches)
            {
                (_, long forkHeight) = LocateBlockInActiveChain(chain[0].PrevBlockHash);

                uint branchHeight = (uint)(chain.Count + forkHeight);
                if (branchHeight > GetCurrentHeight())
                {
                    Logger.Information("Attempting reorg of idx {} to active chain, new height of {} vs. {}", branchIdx,
                        branchHeight,
                        forkHeight);

                    reorged |= TryReorg(chain, branchIdx, (uint)forkHeight);
                }

                branchIdx++;
            }

            return reorged;
        }
    }

    public static bool TryReorg(IList<Block> branch, uint branchIdx, uint forkIdx)
    {
        lock (Mutex)
        {
            var forkBlock = ActiveChain[(int)forkIdx];

            var oldActiveChain = DisconnectToFork(forkBlock);

            Debug.Assert(branch.First().PrevBlockHash == ActiveChain.Last().Id());

            foreach (var block in branch)
                if (ConnectBlock(block, true) != ActiveChainIdx)
                {
                    RollbackReorg(oldActiveChain, forkBlock, branchIdx);

                    return false;
                }

            SideBranches.RemoveAt((int)(branchIdx - 1));
            SideBranches.Add(oldActiveChain);

            Logger.Information("Chain reorganized, new height {} with tip {}", ActiveChain.Count,
                ActiveChain.Last().Id());

            return true;
        }
    }

    public static void RollbackReorg(IList<Block> oldActiveChain,
        Block forkBlock, uint branchIdx)
    {
        lock (Mutex)
        {
            Logger.Error("Reorg of idx {} to active chain failed", branchIdx);

            DisconnectToFork(forkBlock);

            foreach (var block in oldActiveChain)
            {
                long connectedBlockIdx = ConnectBlock(block, true);

                Debug.Assert(connectedBlockIdx == ActiveChainIdx);
            }
        }
    }

    public static (Block, long) LocateBlockInChain(string blockHash, IList<Block> chain)
    {
        lock (Mutex)
        {
            uint height = 0;
            foreach (var block in chain)
            {
                if (block.Id() == blockHash)
                    return (block, height);

                height++;
            }

            return (null, -1);
        }
    }

    public static (Block, long) LocateBlockInActiveChain(string blockHash)
    {
        return LocateBlockInChain(blockHash, ActiveChain);
    }

    public static (Block, long, long) LocateBlockInAllChains(string blockHash)
    {
        lock (Mutex)
        {
            uint chainIdx = 0;
            (var locatedBlock, long locatedBlockHeight) = LocateBlockInActiveChain(blockHash);
            if (locatedBlock != null)
                return (locatedBlock, locatedBlockHeight, chainIdx);
            chainIdx++;

            foreach (var sideChain in SideBranches)
            {
                (locatedBlock, locatedBlockHeight) = LocateBlockInChain(blockHash, sideChain);
                if (locatedBlock != null)
                    return (locatedBlock, locatedBlockHeight, chainIdx);
                chainIdx++;
            }

            return (null, -1, -1);
        }
    }

    public static (TxOut, Tx, long, bool, long) FindTxOutForTxIn(TxIn txIn, IList<Block> chain)
    {
        lock (Mutex)
        {
            for (uint height = 0; height < chain.Count; height++)
                foreach (var tx in chain[(int)height].Txs)
                {
                    var toSpend = txIn.ToSpend;
                    if (toSpend.TxId == tx.Id())
                    {
                        var txOut = tx.TxOuts[(int)toSpend.TxOutIdx];

                        return (txOut, tx, toSpend.TxOutIdx, tx.IsCoinbase(), height);
                    }
                }

            return (null, null, -1, false, -1);
        }
    }

    public static (TxOut, Tx, long, bool, long) FindTxOutForTxInInActiveChain(TxIn txIn)
    {
        return FindTxOutForTxIn(txIn, ActiveChain);
    }

    public static void SaveToDisk()
    {
        lock (Mutex)
        {
            Logger.Information("Saving chain with {} blocks", ActiveChain.Count);

            // TODO: append from previously saved height
            using (var chainOut = new FileStream(ChainPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(chainOut))
                {
                    writer.Write(ActiveChain.Count - 1);
                    for (int height = 1; height < ActiveChain.Count; height++)
                    {
                        byte[] serializedData = ActiveChain[height].Serialize().Buffer;
                        writer.Write((uint)serializedData.Length);
                        writer.Write(serializedData);
                    }
                }
            }
        }
    }

    public static bool LoadFromDisk()
    {
        lock (Mutex)
        {
            if (File.Exists(ChainPath))
                using (var chainIn = new FileStream(ChainPath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(chainIn))
                    {
                        int blockCount = reader.ReadInt32();
                        var loadedChain = new List<Block>();
                        for (int i = 0; i < blockCount; i++)
                        {
                            uint blockSize = reader.ReadUInt32();
                            var block = Block.Deserialize(new BinaryBuffer(reader.ReadBytes((int)blockSize)));
                            if (block == null)
                            {
                                Logger.Error("Load chain failed, starting from genesis");
                                return false;
                            }

                            loadedChain.Add(block);
                        }

                        foreach (var block in loadedChain)
                            if (ConnectBlock(block) != ActiveChainIdx)
                            {
                                ActiveChain.Clear();
                                ActiveChain.Add(GenesisBlock);
                                SideBranches.Clear();
                                UTXO.Map.Clear();
                                Mempool.Map.Clear();

                                Logger.Error("Load chain failed, starting from genesis");
                                return false;
                            }

                        Logger.Information("Loaded chain with {} blocks", ActiveChain.Count);
                        return true;
                    }
                }

            Logger.Error("Load chain failed, starting from genesis");
            return false;
        }
    }
}
