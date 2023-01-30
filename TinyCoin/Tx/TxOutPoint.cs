using System;

namespace TinyCoin.Tx
{
    public class TxOutPoint : ISerializable, IDeserializable, IEquatable<TxOutPoint>
    {
        public string TxId = string.Empty;
        public long TxOutIdx = -1;

        public TxOutPoint(string txId, long txOutIdx)
        {
            TxId = txId;
            TxOutIdx = txOutIdx;
        }

        public bool Deserialize(BinaryBuffer buffer)
        {
            //TODO: rollback if fail

            if (!buffer.Read(ref TxId))
                return false;
            if (!buffer.Read(ref TxOutIdx))
                return false;

            return true;
        }

        public bool Equals(TxOutPoint other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return TxId == other.TxId && TxOutIdx == other.TxOutIdx;
        }

        public BinaryBuffer Serialize()
        {
            var buffer = new BinaryBuffer();

            buffer.Write(TxId);
            buffer.Write(TxOutIdx);

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
            return Equals((TxOutPoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TxId, TxOutIdx);
        }

        public static bool operator ==(TxOutPoint lhs, TxOutPoint rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                    return true;

                return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(TxOutPoint lhs, TxOutPoint rhs)
        {
            return !(lhs == rhs);
        }
    }
}
