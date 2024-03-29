using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;

namespace TinyCoin.Txs;

public class UnspentTxOut : ISerializable, IDeserializable<UnspentTxOut>, IEquatable<UnspentTxOut>,
    ICloneable<UnspentTxOut>
{
    private static readonly ILogger Logger =
        Serilog.Log.ForContext(Constants.SourceContextPropertyName, nameof(UnspentTxOut));

    public static readonly Dictionary<TxOutPoint, UnspentTxOut> Map = new Dictionary<TxOutPoint, UnspentTxOut>();
    public static readonly object Mutex = new object();

    public long Height = -1;
    public bool IsCoinbase;
    public TxOut TxOut;
    public TxOutPoint TxOutPoint;

    public UnspentTxOut()
    {
    }

    public UnspentTxOut(TxOut txOut, TxOutPoint txOutPoint, bool isCoinbase, long height)
    {
        TxOut = txOut;
        TxOutPoint = txOutPoint;
        IsCoinbase = isCoinbase;
        Height = height;
    }

    public static UnspentTxOut Deserialize(BinaryBuffer buffer)
    {
        var unspentTxOut = new UnspentTxOut();
        unspentTxOut.TxOut = TxOut.Deserialize(buffer);
        if (unspentTxOut.TxOut == null)
            return null;
        unspentTxOut.TxOutPoint = TxOutPoint.Deserialize(buffer);
        if (unspentTxOut.TxOutPoint == null)
            return null;
        if (!buffer.Read(ref unspentTxOut.IsCoinbase))
            return null;
        if (!buffer.Read(ref unspentTxOut.Height))
            return null;

        return unspentTxOut;
    }

    public bool Equals(UnspentTxOut other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return TxOut == other.TxOut && TxOutPoint == other.TxOutPoint && IsCoinbase == other.IsCoinbase &&
               Height == other.Height;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteRaw(TxOut.Serialize().Buffer);
        buffer.WriteRaw(TxOutPoint.Serialize().Buffer);
        buffer.Write(IsCoinbase);
        buffer.Write(Height);

        return buffer;
    }

    public static void AddToMap(TxOut txOut, string txId, long idx, bool isCoinbase,
        long height)
    {
        lock (Mutex)
        {
            var txOutPoint = new TxOutPoint(txId, idx);

            var utxo = new UnspentTxOut(txOut, txOutPoint, isCoinbase, height);

            Logger.Debug("Adding TxOutPoint {TransactionId} to UTXO map", utxo.TxOutPoint.TxId);

            Map[utxo.TxOutPoint] = utxo;
        }
    }

    public static void RemoveFromMap(string txId, long idx)
    {
        lock (Mutex)
        {
            var mapIt = Map.Keys.FirstOrDefault(p => p.TxId == txId && p.TxOutIdx == idx);
            if (mapIt != null)
                Map.Remove(mapIt);
        }
    }

    public static UnspentTxOut FindInList(TxIn txIn, IList<Tx> txs)
    {
        foreach (var tx in txs)
        {
            var toSpend = txIn.ToSpend;

            if (tx.Id() == toSpend.TxId)
            {
                if (tx.TxOuts.Count - 1 < toSpend.TxOutIdx)
                    return null;

                var matchingTxOut = tx.TxOuts[(int)toSpend.TxOutIdx];
                var txOutPoint = new TxOutPoint(toSpend.TxId, toSpend.TxOutIdx);
                return new UnspentTxOut(matchingTxOut, txOutPoint, false, -1);
            }
        }

        return null;
    }

    public static UnspentTxOut FindInMap(TxOutPoint toSpend)
    {
        lock (Mutex)
        {
            var mapIt = Map.Keys.FirstOrDefault(p => p == toSpend);
            if (mapIt != null)
                return Map[mapIt];

            return null;
        }
    }

    public static TxOut FindTxOutInBlock(Block block, TxIn txIn)
    {
        foreach (var tx in block.Txs)
            if (tx.Id() == txIn.ToSpend.TxId)
                return tx.TxOuts[(int)txIn.ToSpend.TxOutIdx];

        return null;
    }

    public static TxOut FindTxOutInMap(TxIn txIn)
    {
        lock (Mutex)
        {
            foreach (var utxo in Map.Values)
                if (txIn.ToSpend.TxId == utxo.TxOutPoint.TxId &&
                    txIn.ToSpend.TxOutIdx == utxo.TxOutPoint.TxOutIdx)
                    return utxo.TxOut;

            return null;
        }
    }

    public static TxOut FindTxOutInMapOrBlock(Block block, TxIn txIn)
    {
        var utxo = FindTxOutInMap(txIn);
        if (utxo != null)
            return utxo;

        return FindTxOutInBlock(block, txIn);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((UnspentTxOut)obj);
    }

    public static bool operator ==(UnspentTxOut lhs, UnspentTxOut rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(UnspentTxOut lhs, UnspentTxOut rhs)
    {
        return !(lhs == rhs);
    }
}
