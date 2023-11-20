using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using TinyCoin.BlockChain;
using TinyCoin.Crypto;
using TinyCoin.P2P;
using UTXO = TinyCoin.Txs.UnspentTxOut;

namespace TinyCoin.Txs;

public class Tx : ISerializable, IDeserializable<Tx>, IEquatable<Tx>, ICloneable<Tx>
{
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(Tx));

    public long LockTime;
    public IList<TxIn> TxIns;
    public IList<TxOut> TxOuts;

    public Tx()
    {
        TxIns = new List<TxIn>();
        TxOuts = new List<TxOut>();
    }

    public Tx(IList<TxIn> txIns, IList<TxOut> txOuts, long lockTime)
    {
        TxIns = txIns;
        TxOuts = txOuts;
        LockTime = lockTime;
    }

    public Tx Clone()
    {
        return Deserialize(Serialize());
    }

    public static Tx Deserialize(BinaryBuffer buffer)
    {
        var tx = new Tx();

        uint txInSize = 0;
        if (!buffer.ReadSize(ref txInSize))
            return null;

        tx.TxIns = new List<TxIn>((int)txInSize);
        for (int i = 0; i < txInSize; i++)
            tx.TxIns.Add(TxIn.Deserialize(buffer));

        uint txOutSize = 0;
        if (!buffer.ReadSize(ref txOutSize))
            return null;

        tx.TxOuts = new List<TxOut>((int)txOutSize);
        for (int i = 0; i < txOutSize; i++)
            tx.TxOuts.Add(TxOut.Deserialize(buffer));

        if (!buffer.Read(ref tx.LockTime))
            return null;

        return tx;
    }

    public bool Equals(Tx other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return TxIns.SequenceEqual(other.TxIns) && TxOuts.SequenceEqual(other.TxOuts) && LockTime == other.LockTime;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteSize((uint)TxIns.Count);
        foreach (var txIn in TxIns)
            buffer.WriteRaw(txIn.Serialize().Buffer);
        buffer.WriteSize((uint)TxOuts.Count);
        foreach (var txOut in TxOuts)
            buffer.WriteRaw(txOut.Serialize().Buffer);
        buffer.Write(LockTime);

        return buffer;
    }

    public static Tx CreateCoinbase(string payToAddr, ulong value, long height)
    {
        var txInUnlockSig = new BinaryBuffer();
        txInUnlockSig.Write(height);
        var txIn = new TxIn(null, txInUnlockSig.Buffer, Array.Empty<byte>(), -1);

        var txOut = new TxOut(value, payToAddr);

        var txIns = new List<TxIn> { txIn };
        var txOuts = new List<TxOut> { txOut };
        var tx = new Tx(txIns, txOuts, 0);

        return tx;
    }

    private void ValidateSignatureForSpend(TxIn txIn, UnspentTxOut utxo)
    {
        string pubKeyAsAddr = Wallet.PubKeyToAddress(txIn.UnlockPubKey);
        if (pubKeyAsAddr != utxo.TxOut.ToAddress)
            throw new TxUnlockException("Public key does not match");

        byte[] spendMsg = MsgSerializer.BuildSpendMsg(txIn.ToSpend, txIn.UnlockPubKey, txIn.Sequence, TxOuts);
        if (!ECDSA.VerifySig(txIn.UnlockSig, spendMsg, txIn.UnlockPubKey))
        {
            Logger.Error("Key verification failed");
            throw new TxUnlockException("Signature does not match");
        }
    }

    public void Validate(ValidateRequest req)
    {
        ValidateBasics(req.AsCoinbase);

        ulong avaliableToSpend = 0;
        foreach (var txIn in TxIns)
        {
            var utxo = UTXO.FindInMap(txIn.ToSpend);
            if (utxo == null)
            {
                if (req.SiblingsInBlock.Count != 0)
                    utxo = UTXO.FindInList(txIn, req.SiblingsInBlock);

                if (req.Allow_UTXO_FromMempool)
                    utxo = Mempool.Find_UTXO_InMempool(txIn.ToSpend);

                if (utxo == null)
                    throw new TxValidationException(
                        $"Unable to find any UTXO for TxIn {Id()}, orphaning transaction",
                        this);
            }

            if (utxo.IsCoinbase && Chain.GetCurrentHeight() - utxo.Height < NetParams.CoinbaseMaturity)
                throw new TxValidationException("Coinbase UTXO not ready for spending");

            try
            {
                ValidateSignatureForSpend(txIn, utxo);
            }
            catch (TxUnlockException ex)
            {
                Logger.Error(ex, "");

                throw new TxValidationException($"TxIn {Id()} not a valid spend of UTXO");
            }

            avaliableToSpend += utxo.TxOut.Value;
        }

        ulong totalSpent = 0;
        foreach (var txOut in TxOuts)
            totalSpent += txOut.Value;

        if (avaliableToSpend < totalSpent)
            throw new TxValidationException("Spent value more than available");
    }

    public bool IsCoinbase()
    {
        return TxIns.Count == 1 && TxIns.First().ToSpend == null;
    }

    public string Id()
    {
        return Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Serialize().Buffer));
    }

    public void ValidateBasics(bool coinbase = false)
    {
        if (TxOuts.Count == 0 || (TxIns.Count == 0 && !coinbase))
            throw new TxValidationException("Missing TxOuts or TxIns");

        if (Serialize().Buffer.Length > NetParams.MaxBlockSerializedSizeInBytes)
            throw new TxValidationException("Too large");

        ulong totalSpent = 0;
        foreach (var txOut in TxOuts)
            totalSpent += txOut.Value;

        if (totalSpent > NetParams.MaxMoney)
            throw new TxValidationException("Spent value too high");
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((Tx)obj);
    }

    public static bool operator ==(Tx lhs, Tx rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(Tx lhs, Tx rhs)
    {
        return !(lhs == rhs);
    }

    public class ValidateRequest
    {
        public bool Allow_UTXO_FromMempool = true;
        public bool AsCoinbase = false;
        public IList<Tx> SiblingsInBlock = new List<Tx>();
    }
}
