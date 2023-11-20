using System;
using System.Linq;

namespace TinyCoin.Txs;

public class TxIn : ISerializable, IDeserializable<TxIn>, IEquatable<TxIn>, ICloneable<TxIn>
{
    public int Sequence = -1;
    public TxOutPoint ToSpend;
    public byte[] UnlockPubKey;
    public byte[] UnlockSig;

    public TxIn()
    {
        UnlockPubKey = Array.Empty<byte>();
        UnlockSig = Array.Empty<byte>();
    }

    public TxIn(TxOutPoint toSpend, byte[] unlockSig, byte[] unlockPubKey, int sequence)
    {
        ToSpend = toSpend;
        UnlockSig = unlockSig;
        UnlockPubKey = unlockPubKey;
        Sequence = sequence;
    }

    public TxIn Clone()
    {
        return Deserialize(Serialize());
    }

    public static TxIn Deserialize(BinaryBuffer buffer)
    {
        var txIn = new TxIn();
        bool hasToSpend = false;
        if (!buffer.Read(ref hasToSpend))
            return null;
        if (hasToSpend)
        {
            txIn.ToSpend = TxOutPoint.Deserialize(buffer);
            if (txIn.ToSpend == null)
                return null;
        }

        if (!buffer.Read(ref txIn.UnlockSig))
            return null;

        if (!buffer.Read(ref txIn.UnlockPubKey))
            return null;

        if (!buffer.Read(ref txIn.Sequence))
            return null;

        return txIn;
    }

    public bool Equals(TxIn other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return ToSpend == other.ToSpend && UnlockSig.SequenceEqual(other.UnlockSig) &&
               UnlockPubKey.SequenceEqual(other.UnlockPubKey) &&
               Sequence == other.Sequence;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        bool hasToSpend = ToSpend != null;
        buffer.Write(hasToSpend);
        if (hasToSpend)
            buffer.WriteRaw(ToSpend.Serialize().Buffer);
        buffer.Write(UnlockSig);
        buffer.Write(UnlockPubKey);
        buffer.Write(Sequence);

        return buffer;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((TxIn)obj);
    }

    public static bool operator ==(TxIn lhs, TxIn rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TxIn lhs, TxIn rhs)
    {
        return !(lhs == rhs);
    }
}
