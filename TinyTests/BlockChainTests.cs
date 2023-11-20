using System;
using System.Collections.Generic;
using System.Linq;
using TinyCoin;
using TinyCoin.BlockChain;
using TinyCoin.Crypto;
using TinyCoin.Txs;
using Xunit;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyTests;

public class BlockChainTests
{
    public static IList<Tx> Chain1Block1Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1PMycacnJaSqwwJqjawXBErnLsZ7RkXUAs")
            }, 0)
    };

    public static Block Chain1Block1 = new Block(
        0, "", "a4a241a0b693ad8ee736907fbe3fc572044b380cdc186f80647f1e8354bb2ba7",
        1501821412, 24, 5804256, Chain1Block1Txs);

    public static IList<Tx> Chain1Block2Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain1Block2 = new Block(
        0, "0000001616129fcc8a1240972d1e39a8569c7db3e965e38db70ccd4418815efd",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501826444, 24, 13835058055285570289, Chain1Block2Txs);

    public static IList<Tx> Chain1Block3Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain1Block3 = new Block(
        0, "000000731ba1a0b651182140e8332287186c6a93ddbfc42455c3f88e020a5ce8",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501826556, 24, 9223372036856676382, Chain1Block3Txs);

    public static IList<Block> Chain1 = new List<Block> { Chain1Block1, Chain1Block2, Chain1Block3 };

    public static IList<Tx> Chain2Block2Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain2Block2 = new Block(
        0, "0000001616129fcc8a1240972d1e39a8569c7db3e965e38db70ccd4418815efd",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501826757, 24, 9223372036864717828, Chain2Block2Txs);

    public static IList<Tx> Chain2Block3Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain2Block3 = new Block(
        0, "0000002def15eabb75ecde313f0f1e239592c758362b1f892f80e9c369a23bcc",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501826872, 24, 13835058055284619847, Chain2Block3Txs);

    public static IList<Tx> Chain2Block4Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain2Block4 = new Block(
        0, "0000006249ba6098b7ea1c6b1ab931d36a306a8bda8f3e632b703d84dda8b4b6",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501826949, 24, 4611686018428394083, Chain2Block4Txs);

    public static IList<Tx> Chain2Block5Txs = new List<Tx>
    {
        new Tx(
            new List<TxIn>
            {
                new TxIn(null, Array.Empty<byte>(), Array.Empty<byte>(), -1)
            }, new List<TxOut>
            {
                new TxOut(5000000000, "1Piq91dFUqSb7tdddCWvuGX5UgdzXeoAwA")
            }, 0)
    };

    public static Block Chain2Block5 = new Block(
        0, "0000002dca22d47151d6cb1a2e60c1af535174c0b2b0d4152c63b76d034edc6d",
        "8b0485e1a7823fc2ad3195837146e2b01e860a18f0cf81e524078b0116400430",
        1501827000, 24, 13835058055287732375, Chain2Block5Txs);

    public static IList<Block> Chain2 = new List<Block>
    {
        Chain1Block1, Chain2Block2, Chain2Block3, Chain2Block4, Chain2Block5
    };

    [Fact]
    public void MedianTimePast()
    {
        Chain.ActiveChain.Clear();

        Assert.Equal(0, Chain.GetMedianTimePast(10));

        long[] timestamps = { 1, 30, 60, 90, 400 };

        foreach (long timestamp in timestamps)
        {
            var dummyBlock = new Block(0, "foo", "foo", timestamp, 1, 0, Array.Empty<Tx>());

            Chain.ActiveChain.Add(dummyBlock);
        }

        Assert.Equal(400, Chain.GetMedianTimePast(1));
        Assert.Equal(90, Chain.GetMedianTimePast(3));
        Assert.Equal(90, Chain.GetMedianTimePast(2));
        Assert.Equal(60, Chain.GetMedianTimePast(5));
    }

    [Fact]
    public void Reorg()
    {
        Chain.ActiveChain.Clear();
        Chain.SideBranches.Clear();
        MemPool.Map.Clear();
        UTXO.Map.Clear();

        foreach (var block in Chain1)
            Assert.Equal(Chain.ActiveChainIdx, Chain.ConnectBlock(block));

        Chain.SideBranches.Clear();
        MemPool.Map.Clear();
        UTXO.Map.Clear();

        foreach (var block in Chain.ActiveChain)
        foreach (var tx in block.Txs)
            for (uint i = 0; i < tx.TxOuts.Count; i++)
                UTXO.AddToMap(tx.TxOuts[(int)i], tx.Id(), i, tx.IsCoinbase(), Chain.ActiveChain.Count);

        Assert.Equal(3, UTXO.Map.Count);
        Assert.False(Chain.ReorgIfNecessary());

        Assert.Equal(1, Chain.ConnectBlock(Chain2[1]));

        Assert.False(Chain.ReorgIfNecessary());
        Assert.Single(Chain.SideBranches);
        Assert.Equal(Chain2[1], Chain.SideBranches[0][0]);
        Assert.Equal(Chain1.Count, Chain.ActiveChain.Count);
        for (uint i = 0; i < Chain.ActiveChain.Count; i++)
            Assert.Equal(Chain1[(int)i], Chain.ActiveChain[(int)i]);
        Assert.True(MemPool.Map.Count == 0);
        string[] txIds = { "b6678c", "b90f9b", "b6678c" };
        Assert.Equal(txIds.Length, UTXO.Map.Count);
        foreach (var k in UTXO.Map.Keys)
        {
            bool found = false;
            foreach (string txId in txIds)
            {
                found |= k.TxId.EndsWith(txId);
                if (found)
                    break;
            }

            Assert.True(found);
        }

        Assert.Equal(1, Chain.ConnectBlock(Chain2[2]));

        Assert.False(Chain.ReorgIfNecessary());
        Assert.Single(Chain.SideBranches);
        var sideBranchTest = new List<Block> { Chain2[1], Chain2[2] };
        for (uint i = 0; i < Chain.SideBranches[0].Count; i++)
            Assert.Equal(sideBranchTest[(int)i], Chain.SideBranches[0][(int)i]);
        Assert.Equal(Chain.ActiveChain.Count, Chain1.Count);
        for (uint i = 0; i < Chain.ActiveChain.Count; i++)
            Assert.Equal(Chain1[(int)i], Chain.ActiveChain[(int)i]);
        Assert.True(MemPool.Map.Count == 0);
        Assert.Equal(txIds.Length, UTXO.Map.Count);
        foreach (var k in UTXO.Map.Keys)
        {
            bool found = false;
            foreach (string txId in txIds)
            {
                found |= k.TxId.EndsWith(txId);
                if (found)
                    break;
            }

            Assert.True(found);
        }

        var chain3Faulty = Chain2.Select(block => block.Clone()).ToList();
        var chain2Block4Copy = Chain2Block4.Clone();
        chain2Block4Copy.Nonce = 1;
        chain3Faulty[3] = chain2Block4Copy;

        Assert.Equal(-1, Chain.ConnectBlock(chain3Faulty[3]));
        Assert.False(Chain.ReorgIfNecessary());

        Assert.Single(Chain.SideBranches);
        for (uint i = 0; i < Chain.SideBranches[0].Count; i++)
            Assert.Equal(sideBranchTest[(int)i], Chain.SideBranches[0][(int)i]);
        Assert.Equal(Chain.ActiveChain.Count, Chain1.Count);
        for (uint i = 0; i < Chain.ActiveChain.Count; i++)
            Assert.Equal(Chain1[(int)i], Chain.ActiveChain[(int)i]);
        Assert.True(MemPool.Map.Count == 0);
        Assert.Equal(txIds.Length, UTXO.Map.Count);
        foreach (var k in UTXO.Map.Keys)
        {
            bool found = false;
            foreach (string txId in txIds)
            {
                found |= k.TxId.EndsWith(txId);
                if (found)
                    break;
            }

            Assert.True(found);
        }

        Assert.Equal(1, Chain.ConnectBlock(Chain2[3]));
        Assert.Equal(1, Chain.ConnectBlock(Chain2[4]));

        Assert.Single(Chain.SideBranches);
        Assert.Equal(2, Chain.SideBranches[0].Count);
        var sideBranchIds = new List<string>(Chain.SideBranches[0].Count);
        foreach (var block in Chain.SideBranches[0])
            sideBranchIds.Add(block.Id());
        var chain1Ids = new List<string>(Chain1.Count);
        for (uint i = 1; i < Chain1.Count; i++)
            chain1Ids.Add(Chain1[(int)i].Id());
        Assert.Equal(chain1Ids, sideBranchIds);
        var sideBranchTest2 = new List<Block> { Chain1[1], Chain1[2] };
        for (uint i = 0; i < Chain.SideBranches[0].Count; i++)
            Assert.Equal(sideBranchTest2[(int)i], Chain.SideBranches[0][(int)i]);
        Assert.True(MemPool.Map.Count == 0);
        string[] txIds2 = { "b90f9b", "b6678c", "b6678c", "b6678c", "b6678c" };
        Assert.Equal(UTXO.Map.Count, txIds2.Length);
        foreach (var k in UTXO.Map.Keys)
        {
            bool found = false;
            foreach (string txId in txIds2)
            {
                found |= k.TxId.EndsWith(txId);
                if (found)
                    break;
            }

            Assert.True(found);
        }
    }

#if !DEBUG
    [Fact]
    public void DependentTxsInSingleBlock()
    {
        Chain.ActiveChain.Clear();
        Chain.SideBranches.Clear();
        MemPool.Map.Clear();
        UTXO.Map.Clear();

        Assert.Equal(Chain.ActiveChainIdx, Chain.ConnectBlock(Chain1[0]));
        Assert.Equal(Chain.ActiveChainIdx, Chain.ConnectBlock(Chain1[1]));

        Assert.Equal(2, Chain.ActiveChain.Count);
        Assert.Equal(2, UTXO.Map.Count);

        byte[] privKey = Utils.HexStringToByteArray("18e14a7b6a307f426a94f8114701e7c8e774e7f9a47e2c2035db29a206321725");
        byte[] pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
        string address = Wallet.PubKeyToAddress(pubKey);

        var utxo1 = UTXO.Map.First().Value;
        var txOut1 = new TxOut(901, utxo1.TxOut.ToAddress);
        var txOuts1 = new List<TxOut> { txOut1 };
        var txIn1 = Wallet.BuildTxIn(privKey, utxo1.TxOutPoint, txOuts1);
        var tx1 = new Tx(new List<TxIn> { txIn1 }, txOuts1, 0);

        Assert.Throws<TxValidationException>(() =>
        {
            try
            {
                tx1.Validate(new Tx.ValidateRequest());
            }
            catch (TxValidationException ex)
            {
                Assert.Equal("Coinbase UTXO not ready for spending", ex.Message);
                throw;
            }
        });

        Chain.ConnectBlock(Chain1[2]);

        MemPool.AddTxToMemPool(tx1);
        Assert.True(MemPool.Map.ContainsKey(tx1.Id()));

        var txOut2 = new TxOut(9001, txOut1.ToAddress);
        var txOuts2 = new List<TxOut> { txOut2 };
        var txOutPoint2 = new TxOutPoint(tx1.Id(), 0);
        var txIn2 = Wallet.BuildTxIn(privKey, txOutPoint2, txOuts2);
        var tx2 = new Tx(new List<TxIn> { txIn2 }, txOuts2, 0);

        MemPool.AddTxToMemPool(tx2);
        Assert.False(MemPool.Map.ContainsKey(tx2.Id()));

        Assert.Throws<TxValidationException>(() =>
        {
            try
            {
                tx2.Validate(new Tx.ValidateRequest());
            }
            catch (TxValidationException ex)
            {
                Assert.Equal("Spent value more than available", ex.Message);
                throw;
            }
        });

        txOut2.Value = 901;
        txIn2 = Wallet.BuildTxIn(privKey, txOutPoint2, txOuts2);
        tx2.TxIns[0] = txIn2;

        MemPool.AddTxToMemPool(tx2);
        Assert.True(MemPool.Map.ContainsKey(tx2.Id()));

        var block = PoW.AssembleAndSolveBlock(address);

        Assert.Equal(Chain.ActiveChainIdx, Chain.ConnectBlock(block));

        Assert.Equal(Chain.ActiveChain.Last(), block);
        Assert.Equal(2, block.Txs.Count - 1);
        var txs = new[] { tx1, tx2 };
        for (uint i = 0; i < txs.Length; i++)
            Assert.Equal(txs[(int)i], block.Txs[(int)(i + 1)]);
        Assert.False(MemPool.Map.ContainsKey(tx1.Id()));
        Assert.False(MemPool.Map.ContainsKey(tx2.Id()));
        var mapIt1 =
            UTXO.Map.Keys.FirstOrDefault(txOutPoint => txOutPoint.TxId == tx1.Id() && txOutPoint.TxOutIdx == 0);
        Assert.Null(mapIt1);

        var mapIt2 =
            UTXO.Map.Keys.FirstOrDefault(txOutPoint => txOutPoint.TxId == tx2.Id() && txOutPoint.TxOutIdx == 0);
        Assert.NotNull(mapIt2);
    }

    [Fact]
    public void MinerTransaction()
    {
        Chain.ActiveChain.Clear();
        Chain.SideBranches.Clear();
        MemPool.Map.Clear();
        UTXO.Map.Clear();

        (byte[] minerPrivKey, _, string minerAddress) = Wallet.InitWallet("miner.dat");
        (_, _, string receiverAddress) = Wallet.InitWallet("receiver.dat");

        var firstBlock = PoW.AssembleAndSolveBlock(minerAddress);
        Assert.NotNull(firstBlock);
        Chain.ConnectBlock(firstBlock);
        Chain.SaveToDisk();

        for (int i = 0; i < NetParams.CoinbaseMaturity + 1; i++)
        {
            var maturityBlocks = PoW.AssembleAndSolveBlock(minerAddress);
            Assert.NotNull(maturityBlocks);
            Chain.ConnectBlock(maturityBlocks);
            Chain.SaveToDisk();
        }

        Assert.True(Wallet.GetBalance_Miner(minerAddress) > 0);
        var tx = Wallet.SendValue_Miner(firstBlock.Txs.First().TxOuts.First().Value / 2, 100, receiverAddress,
            minerPrivKey);
        Assert.NotNull(tx);
        Assert.Equal(TxStatus.MemPool, Wallet.GetTxStatus_Miner(tx.Id()).Status);

        var postTxBlock = PoW.AssembleAndSolveBlock(minerAddress);
        Assert.NotNull(postTxBlock);
        Chain.ConnectBlock(postTxBlock);
        Chain.SaveToDisk();

        var minedTxStatus = Wallet.GetTxStatus_Miner(tx.Id());
        Assert.Equal(TxStatus.Mined, minedTxStatus.Status);
        Assert.Equal(minedTxStatus.BlockId, postTxBlock.Id());
        Assert.True(Wallet.GetBalance_Miner(receiverAddress) > 0);
    }
#endif
}
