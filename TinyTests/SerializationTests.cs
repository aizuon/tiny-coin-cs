using System;
using System.Collections.Generic;
using TinyCoin.Txs;
using Xunit;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyTests;

public class SerializationTests
{
    [Fact]
    public void TxSerialization()
    {
        var txIns = new List<TxIn>();
        var toSpend = new TxOutPoint("foo", 0);
        var txIn = new TxIn(toSpend, Array.Empty<byte>(), Array.Empty<byte>(), -1);
        txIns.Add(txIn);

        var txOuts = new List<TxOut>();
        var txOut = new TxOut(0, "foo");
        txOuts.Add(txOut);

        var tx = new Tx(txIns, txOuts, 0);

        var serializedBuffer = tx.Serialize();

        var tx2 = Tx.Deserialize(serializedBuffer);
        Assert.NotNull(tx2);

        Assert.Single(tx2.TxIns);
        var txIn2 = tx2.TxIns[0];
        Assert.Equal(txIn.Sequence, txIn2.Sequence);
        var toSpend2 = txIn2.ToSpend;
        Assert.Equal(toSpend.TxId, toSpend2.TxId);
        Assert.Equal(toSpend.TxOutIdx, toSpend2.TxOutIdx);
        Assert.Equal(txIn.UnlockPubKey, txIn2.UnlockPubKey);
        Assert.Equal(txIn.UnlockSig, txIn2.UnlockSig);

        Assert.Single(tx2.TxOuts);
        var txOut2 = tx2.TxOuts[0];
        Assert.Equal(txOut.ToAddress, txOut2.ToAddress);
        Assert.Equal(txOut.Value, txOut2.Value);

        Assert.Equal(tx.LockTime, tx2.LockTime);
    }

    [Fact]
    public void TxInSerialization()
    {
        var toSpend = new TxOutPoint("foo", 0);
        var txIn = new TxIn(toSpend, Array.Empty<byte>(), Array.Empty<byte>(), -1);

        var serializedBuffer = txIn.Serialize();

        var txIn2 = TxIn.Deserialize(serializedBuffer);
        Assert.NotNull(txIn2);

        Assert.Equal(txIn.Sequence, txIn2.Sequence);

        var toSpend2 = txIn2.ToSpend;
        Assert.Equal(toSpend.TxId, toSpend2.TxId);
        Assert.Equal(toSpend.TxOutIdx, toSpend2.TxOutIdx);

        Assert.Equal(txIn.UnlockPubKey, txIn2.UnlockPubKey);
        Assert.Equal(txIn.UnlockSig, txIn2.UnlockSig);
    }

    [Fact]
    public void TxOutSerialization()
    {
        var txOut = new TxOut(0, "foo");

        var serializedBuffer = txOut.Serialize();

        var txOut2 = TxOut.Deserialize(serializedBuffer);
        Assert.NotNull(txOut2);

        Assert.Equal(txOut.ToAddress, txOut2.ToAddress);
        Assert.Equal(txOut.Value, txOut2.Value);
    }

    [Fact]
    public void TxOutPointSerialization()
    {
        var txOutPoint = new TxOutPoint("foo", 0);

        var serializedBuffer = txOutPoint.Serialize();

        var txOutPoint2 = TxOutPoint.Deserialize(serializedBuffer);
        Assert.NotNull(txOutPoint2);

        Assert.Equal(txOutPoint.TxId, txOutPoint2.TxId);
        Assert.Equal(txOutPoint.TxOutIdx, txOutPoint2.TxOutIdx);
    }

    [Fact]
    public void UnspentTxOutSerialization()
    {
        var txOut = new TxOut(0, "foo");

        var txOutPoint = new TxOutPoint("foo", 0);

        var utxo = new UTXO(txOut, txOutPoint, false, 0);

        var serializedBuffer = utxo.Serialize();

        var utxo2 = UTXO.Deserialize(serializedBuffer);
        Assert.NotNull(utxo2);

        var txOut2 = utxo.TxOut;
        Assert.Equal(txOut.ToAddress, txOut2.ToAddress);
        Assert.Equal(txOut.Value, txOut2.Value);

        var txOutPoint2 = utxo.TxOutPoint;
        Assert.Equal(txOutPoint.TxId, txOutPoint2.TxId);
        Assert.Equal(txOutPoint.TxOutIdx, txOutPoint2.TxOutIdx);

        Assert.Equal(utxo.IsCoinbase, utxo2.IsCoinbase);
        Assert.Equal(utxo.Height, utxo2.Height);
    }
}
