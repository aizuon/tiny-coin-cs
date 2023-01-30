using TinyCoin;
using Xunit;

namespace TinyTests
{
    public class CryptoTests
    {
        [Fact]
        public void SHA256_Hashing()
        {
            string hash = Utils.ByteArrayToHexString(SHA256.HashBinary(Utils.StringToByteArray("foo")));

            Assert.Equal("2c26b46b68ffc68ff99b453c1d30413413422d706483bfa0f98a5e886266e7ae", hash);
        }

        [Fact]
        public void SHA256d_Hashing()
        {
            string hash = Utils.ByteArrayToHexString(SHA256.DoubleHashBinary(Utils.StringToByteArray("foo")));

            Assert.Equal("c7ade88fc7a21498a6a5e5c385e1f68bed822b72aa63c4a9a48a02c2466ee29e", hash);
        }

        [Fact]
        public void RIPEMD160_Hashing()
        {
            string hash = Utils.ByteArrayToHexString(RIPEMD160.HashBinary(Utils.StringToByteArray("foo")));

            Assert.Equal("42cfa211018ea492fdee45ac637b7972a0ad6873", hash);
        }

        [Fact]
        public void Base58_Encode()
        {
            string hash = Base58.Encode(Utils.StringToByteArray("foo"));

            Assert.Equal("bQbp", hash);
        }

        [Fact]
        public void ECDSA_KeyPairGeneration()
        {
            (byte[] privKey, byte[] pubKey) = ECDSA.Generate();
            Assert.NotEmpty(privKey);
            Assert.NotEmpty(pubKey);
        }

        [Fact]
        public void ECDSA_GetPubKeyFromPrivKey()
        {
            byte[] privKey =
                Utils.HexStringToByteArray("18e14a7b6a307f426a94f8114701e7c8e774e7f9a47e2c2035db29a206321725");

            byte[] pubKey = ECDSA.GetPubKeyFromPrivKey(privKey);
            Assert.Equal(
                Utils.HexStringToByteArray("0250863ad64a87ae8a2fe83c1af1a8403cb53f53e486d8511dad8a04887e5b2352"),
                pubKey);
        }

        [Fact]
        public void ECDSA_GenerateKeyPairAndGetPubKeyFromPrivKey()
        {
            (byte[] privKey, byte[] pubKey) = ECDSA.Generate();

            string pubKeyStr = Utils.ByteArrayToHexString(pubKey);

            string pubKeyFromPrivKey = Utils.ByteArrayToHexString(ECDSA.GetPubKeyFromPrivKey(privKey));

            Assert.NotEmpty(pubKeyStr);
            Assert.NotEmpty(pubKeyFromPrivKey);
            Assert.Equal(pubKeyStr, pubKeyFromPrivKey);
        }

        [Fact]
        public void ECDSA_SigningAndVerification()
        {
            (byte[] privKey, byte[] pubKey) = ECDSA.Generate();

            string msg = "foo";

            byte[] msgArr = Utils.StringToByteArray(msg);
            byte[] sig = ECDSA.SignMsg(msgArr, privKey);

            Assert.NotEmpty(sig);
            Assert.True(ECDSA.VerifySig(sig, msgArr, pubKey));
        }
    }
}
