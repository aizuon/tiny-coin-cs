using System;
using System.Collections.Generic;
using System.Linq;
using TinyCoin.Crypto;
using TinyCoin.Txs;

namespace TinyCoin.BlockChain;

public class Block : ISerializable, IDeserializable<Block>, IEquatable<Block>, ICloneable<Block>
{
    public byte Bits;
    public string MerkleHash;

    public ulong Nonce;

    public string PrevBlockHash;

    public long Timestamp = -1;

    public IList<Tx> Txs;
    public ulong Version;

    public Block()
    {
        MerkleHash = string.Empty;
        PrevBlockHash = string.Empty;
        Txs = new List<Tx>();
    }

    public Block(ulong version, string prevBlockHash, string merkleHash, long timestamp,
        byte bits, ulong nonce, IList<Tx> txs)
    {
        Version = version;
        PrevBlockHash = prevBlockHash;
        MerkleHash = merkleHash;
        Timestamp = timestamp;
        Bits = bits;
        Nonce = nonce;
        Txs = txs;
    }

    public Block Clone()
    {
        return Deserialize(Serialize());
    }

    public static Block Deserialize(BinaryBuffer buffer)
    {
        var block = new Block();

        if (!buffer.Read(ref block.Version))
            return null;

        if (!buffer.Read(ref block.PrevBlockHash))
            return null;

        if (!buffer.Read(ref block.MerkleHash))
            return null;

        if (!buffer.Read(ref block.Timestamp))
            return null;

        if (!buffer.Read(ref block.Bits))
            return null;

        if (!buffer.Read(ref block.Nonce))
            return null;

        uint txsSize = 0;
        if (!buffer.ReadSize(ref txsSize))
            return null;
        block.Txs = new List<Tx>((int)txsSize);
        for (int i = 0; i < txsSize; i++)
            block.Txs.Add(Tx.Deserialize(buffer));

        return block;
    }

    public bool Equals(Block other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Txs.SequenceEqual(other.Txs) && Version == other.Version && PrevBlockHash == other.PrevBlockHash &&
               MerkleHash == other.MerkleHash && Timestamp == other.Timestamp && Bits == other.Bits &&
               Nonce == other.Nonce;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = Header();

        buffer.WriteSize((uint)Txs.Count);
        foreach (var tx in Txs)
            buffer.WriteRaw(tx.Serialize().Buffer);

        return buffer;
    }

    public BinaryBuffer Header(ulong nonce = 0)
    {
        var buffer = new BinaryBuffer();

        buffer.Write(Version);

        buffer.Write(PrevBlockHash);
        buffer.Write(MerkleHash);

        buffer.Write(Timestamp);

        buffer.Write(Bits);

        buffer.Write(nonce == 0 ? Nonce : nonce);

        return buffer;
    }

    public string Id()
    {
        return Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Header().Buffer));
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((Block)obj);
    }

    public static bool operator ==(Block lhs, Block rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(Block lhs, Block rhs)
    {
        return !(lhs == rhs);
    }
}
