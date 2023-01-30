using System;

namespace TinyCoin.Tx
{
    public class TxOut : ISerializable, IDeserializable, IEquatable<TxOut>
    {
        public string ToAddress = string.Empty;
        public ulong Value;

        public TxOut(ulong value, string toAddress)
        {
            Value = value;
            ToAddress = toAddress;
        }

        public bool Deserialize(BinaryBuffer buffer)
        {
            //TODO: rollback if fail

            if (!buffer.Read(ref Value))
                return false;
            if (!buffer.Read(ref ToAddress))
                return false;

            return true;
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

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, ToAddress);
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
}
