using System.Collections.Generic;
using TinyCoin;
using TinyCoin.BlockChain;
using TinyCoin.Crypto;
using Xunit;

namespace TinyTests
{
    public class MerkleTreeTests
    {
        [Fact]
        public void OneChain()
        {
            string foo = "foo";
            string bar = "bar";
            var tree = new List<string> {foo, bar};

            var root = MerkleTree.GetRoot(tree);
            string fooH = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(foo)));
            string barH = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(bar)));

            string combinedH =
                Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray($"{fooH}{barH}")));

            Assert.Equal(combinedH, root.Value);
            Assert.Equal(fooH, root.Children[0].Value);
            Assert.Equal(barH, root.Children[1].Value);
        }

        [Fact]
        public void TwoChain()
        {
            string foo = "foo";
            string bar = "bar";
            string baz = "baz";
            var tree = new List<string> {foo, bar, baz};

            var root = MerkleTree.GetRoot(tree);
            string fooH = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(foo)));
            string barH = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(bar)));
            string bazH = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray(baz)));

            Assert.Equal(2, root.Children.Count);

            string combinedH1 =
                Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray($"{fooH}{barH}")));
            string combinedH2 =
                Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray($"{bazH}{bazH}")));

            Assert.Equal(combinedH1, root.Children[0].Value);
            Assert.Equal(combinedH2, root.Children[1].Value);
        }
    }
}
