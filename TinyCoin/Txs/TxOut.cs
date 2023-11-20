using System;

namespace TinyCoin.Txs;

public class TxOut : ISerializable, IDeserializable<TxOut>, IEquatable<TxOut>, ICloneable<TxOut>
{
    public string ToAddress;
    public ulong Value;

    public TxOut()
    {
        ToAddress = string.Empty;
    }

    public TxOut(ulong value, string toAddress)
    {
        Value = value;
        ToAddress = toAddress;
    }

    public static TxOut Deserialize(BinaryBuffer buffer)
    {
        var txOut = new TxOut();
        if (!buffer.Read(ref txOut.Value))
            return null;
        if (!buffer.Read(ref txOut.ToAddress))
            return null;

        return txOut;
    }

    public bool Equals(TxOut other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Value == other.Value && ToAddress == other.ToAddress;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(Value);
        buffer.Write(ToAddress);

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
        return Equals((TxOut)obj);
    }

    public static bool operator ==(TxOut lhs, TxOut rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TxOut lhs, TxOut rhs)
    {
        return !(lhs == rhs);
    }
}
