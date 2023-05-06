using System.Collections.Generic;
using System.Linq;
using TinyCoin.Crypto;

namespace TinyCoin.BlockChain
{
    public class MerkleNode
    {
        public IList<MerkleNode> Children;
        public string Value;

        public MerkleNode(string value)
        {
            Value = value;
            Children = new List<MerkleNode>();
        }

        public MerkleNode(string value, IList<MerkleNode> children)
        {
            Value = value;
            Children = children;
        }
    }

    public class MerkleTree
    {
        public static MerkleNode GetRoot(IList<string> leaves)
        {
            var nodes = new List<MerkleNode>(leaves.Count);
            foreach (string l in leaves)
            {
                var node = new MerkleNode(
                    Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(l))));
                nodes.Add(node);
            }

            return FindRoot(nodes);
        }

        //public static MerkleNode GetRootOfTxs(List<Tx> txs);

        private static IList<IList<MerkleNode>> Chunk(IList<MerkleNode> nodes, uint chunkSize)
        {
            var chunks = new List<IList<MerkleNode>>();

            var chunk = new List<MerkleNode>((int)chunkSize);
            foreach (var node in nodes)
            {
                chunk.Add(node);
                if (chunk.Count == chunkSize)
                {
                    chunks.Add(chunk);
                    chunk = new List<MerkleNode>((int)chunkSize);
                }
            }

            if (chunk.Count != 0)
            {
                while (chunk.Count != chunkSize)
                    chunk.Add(chunk.Last());
                chunks.Add(chunk);
            }

            return chunks;
        }

        private static MerkleNode FindRoot(IList<MerkleNode> nodes)
        {
            var chunks = Chunk(nodes, 2);
            var newLevel = new List<MerkleNode>(chunks.Count);
            foreach (var chunk in chunks)
            {
                string combinedId = string.Empty;
                foreach (var node in chunk)
                    combinedId += node.Value;

                string combinedHash =
                    Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(combinedId)));

                var newNode = new MerkleNode(combinedHash, chunk);

                newLevel.Add(newNode);
            }

            return newLevel.Count > 1 ? FindRoot(newLevel) : newLevel.First();
        }
    }
}
