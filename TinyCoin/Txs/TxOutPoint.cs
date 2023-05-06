using System;

namespace TinyCoin.Txs
{
    public class TxOutPoint : ISerializable, IDeserializable<TxOutPoint>, IEquatable<TxOutPoint>
    {
        public string TxId = string.Empty;
        public long TxOutIdx = -1;

        public TxOutPoint()
        {
        }

        public TxOutPoint(string txId, long txOutIdx)
        {
            TxId = txId;
            TxOutIdx = txOutIdx;
        }

        public static TxOutPoint Deserialize(BinaryBuffer buffer)
        {
            var txOutPoint = new TxOutPoint();
            if (!buffer.Read(ref txOutPoint.TxId))
                return null;
            if (!buffer.Read(ref txOutPoint.TxOutIdx))
                return null;

            return txOutPoint;
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
