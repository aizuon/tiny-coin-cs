using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using TinyCoin.Crypto;
using TinyCoin.P2P;
using TinyCoin.P2P.Messages;
using TinyCoin.Txs;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.BlockChain;

public static class PoW
{
    private static readonly ILogger Logger = Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(PoW));
    public static AtomicBool MineInterrupt = new AtomicBool(false);

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

    public static Block AssembleAndSolveBlock(string payCoinbaseToAddress, IList<Tx> txs = null)
    {
        if (txs == null)
            txs = new List<Tx>();

        string prevBlockHash;
        lock (Chain.Mutex)
        {
            prevBlockHash = Chain.ActiveChain.Count != 0 ? Chain.ActiveChain.Last().Id() : "";
        }

        var block = new Block(0, prevBlockHash, "", Utils.GetUnixTimestamp(),
            GetNextWorkRequired(prevBlockHash), 0, txs);

        if (block.Txs.Count == 0)
            block = MemPool.SelectFromMemPool(block);

        ulong fees = CalculateFees(block);
        var coinbaseTx = Tx.CreateCoinbase(payCoinbaseToAddress, GetBlockSubsidy() + fees,
            Chain.ActiveChain.Count);
        block.Txs.Insert(0, coinbaseTx);
        block.MerkleHash = MerkleTree.GetRootOfTxs(block.Txs).Value;

        if (block.Serialize().Buffer.Length > NetParams.MaxBlockSerializedSizeInBytes)
            throw new Exception("Transactions specified create a block too large");

        Logger.Information("Start mining block {BlockId} with {Fee} fees", block.Id(), fees);

        return Mine(block);
    }

    public static Block Mine(Block block)
    {
        if (MineInterrupt.Value)
            MineInterrupt.Value = false;

        var newBlock = block.Clone();
        newBlock.Nonce = 0;
        var targetHash = BigInteger.One << (byte.MaxValue - newBlock.Bits);
        int numThreads = Environment.ProcessorCount - 3;
        if (numThreads <= 0)
            numThreads = 1;

        ulong chunkSize = ulong.MaxValue / (ulong)numThreads;

        var found = new AtomicBool(false);
        var foundNonce = new AtomicULong(0);
        var hashCount = new AtomicULong(0);

        long start = Utils.GetUnixTimestamp();

        var tasks = new List<Task>();

        for (int i = 0; i < numThreads; i++)
        {
            ulong min = ulong.MinValue + chunkSize * (ulong)i;
            tasks.Add(Task.Run(() => MineChunk(newBlock, targetHash, min, chunkSize, found, foundNonce, hashCount)));
        }

        Task.WaitAll(tasks.ToArray());

        if (MineInterrupt.Value)
        {
            MineInterrupt.Value = false;

            Logger.Information("Mining interrupted");

            return null;
        }

        if (!found.Value)
        {
            Logger.Error("No nonce satisfies required bits");

            return null;
        }

        newBlock.Nonce = foundNonce.Value;
        long duration = Utils.GetUnixTimestamp() - start;
        if (duration == 0)
            duration = 1;
        ulong khs = hashCount.Value / (ulong)duration / 1000;
        Logger.Information("Block found => {Time} s, {HashSpeed} kH/s, {BlockId}, {Nonce}", duration, khs,
            newBlock.Id(), newBlock.Nonce);

        return newBlock;
    }

    public static void MineChunk(Block block, BigInteger targetHash, ulong start, ulong chunkSize, AtomicBool found,
        AtomicULong foundNonce, AtomicULong hashCount)
    {
        ulong i = 0;
        while (!HashChecker.IsValid(
                   Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(block.Header(start + i).Buffer)),
                   targetHash))
        {
            hashCount.Increment();

            i++;
            if (found.Value || i == chunkSize || MineInterrupt.Value)
                return;
        }

        found.Value = true;
        foundNonce.Value = start + i;
    }

    public static void MineForever(string payCoinbaseToAddress)
    {
        Chain.LoadFromDisk();

        if (NetClient.SendMsgRandom(new GetBlockMsg(Chain.ActiveChain.Last().Id())))
        {
            Logger.Information("Starting initial block sync");

            long start = Utils.GetUnixTimestamp();
            while (!Chain.InitialBlockDownloadComplete.Value)
            {
                if (Utils.GetUnixTimestamp() - start > 60)
                {
                    // TODO: if sync has started but hasnt finished in time, cancel sync and reset chain

                    Logger.Error("Timeout on initial block sync");

                    break;
                }

                Thread.Sleep(16);
            }
        }

        (byte[] privKey, byte[] pubKey, string myAddress) = Wallet.InitWallet();
        while (true)
        {
            var block = AssembleAndSolveBlock(myAddress);

            if (block != null)
            {
                Chain.ConnectBlock(block);
                Chain.SaveToDisk();
            }
        }
    }

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
