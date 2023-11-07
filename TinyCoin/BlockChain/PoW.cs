using System;
using Serilog;
using Serilog.Core;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.BlockChain;

public static class PoW
{
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(PoW));
    public static volatile bool MineInterrupt = false;

    public static byte GetNextWorkRequired(string prevBlockHash)
    {
        if (prevBlockHash.Length == 0)
            return NetParams.InitialDifficultyBits;

        (var prevBlock, long prevBlockHeight, long _) = Chain.LocateBlockInAllChains(prevBlockHash);
        if ((prevBlockHeight + 1) % NetParams.DifficultyPeriodInBlocks != 0)
            return prevBlock.Bits;

        lock (Chain.Mutex)
        {
            var periodStartBlock = Chain.ActiveChain[(int)Math.Max(
                prevBlockHeight - (NetParams.DifficultyPeriodInBlocks - 1), 0L)];

            long actualTimeTaken = prevBlock.Timestamp - periodStartBlock.Timestamp;
            if (actualTimeTaken < NetParams.DifficultyPeriodInSecsTarget)
                return (byte)(prevBlock.Bits + 1);
            if (actualTimeTaken > NetParams.DifficultyPeriodInSecsTarget)
                return (byte)(prevBlock.Bits - 1);
            return prevBlock.Bits;
        }
    }

    // public static Block AssembleAndSolveBlock(string payCoinbaseToAddress, IList<Tx> txs = null)
    // {
    // }

    // public static Block Mine(Block block)
    // {
    // }

    // public void MineChunk(Block block, uint256_t target_hash, ulong start, ulong chunk_size, std::atomic_bool& found, std::atomic<uint64_t>& foundNonce, std::atomic<uint64_t>& hashCount)
    // {
    // }

    // public static void MineForever(string payCoinbaseToAddress)
    // {
    // }

    public static ulong CalculateFees(Block block)
    {
        ulong fee = 0;

        foreach (var tx in block.Txs)
        {
            ulong spent = 0;
            foreach (var txIn in tx.TxIns)
            {
                var utxo = UTXO.FindTxOutInMapOrBlock(block, txIn);
                if (utxo != null)
                    spent += utxo.Value;
            }

            ulong sent = 0;
            foreach (var txOut in tx.TxOuts)
                sent += txOut.Value;

            fee += spent - sent;
        }

        return fee;
    }

    public static ulong CalculateFees(Tx tx)
    {
        ulong spent = 0;
        foreach (var txIn in tx.TxIns)
        {
            var utxo = UTXO.FindTxOutInMap(txIn);
            if (utxo != null)
                spent += utxo.Value;
        }

        ulong sent = 0;
        foreach (var txOut in tx.TxOuts)
            sent += txOut.Value;

        return spent - sent;
    }

    public static ulong GetBlockSubsidy()
    {
        uint halvings = (uint)(Chain.ActiveChain.Count / NetParams.HalveSubsidyAfterBlocksNum);

        if (halvings >= 64)
            return 0;

        return (ulong)(50 * NetParams.Coin / Math.Pow(2, halvings));
    }
}
