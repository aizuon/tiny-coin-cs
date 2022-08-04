using Org.BouncyCastle.Utilities.Encoders;
using TinyCoin;
using Xunit;

namespace TinyTests
{
    public class WalletTests
    {
        [Fact]
        public void GenPubAndPrivKey()
        {
            (byte[] priv, byte[] pub) = ECDSA.Generate();
            Assert.NotEmpty(priv);
            Assert.NotEmpty(pub);
        }

        [Fact]
        public void GenPubFromPriv()
        {
            byte[] priv = Hex.Decode("18e14a7b6a307f426a94f8114701e7c8e774e7f9a47e2c2035db29a206321725");

            byte[] pub = ECDSA.GetPubKeyFromPrivKey(priv);
            Assert.Equal(Hex.Decode("0250863ad64a87ae8a2fe83c1af1a8403cb53f53e486d8511dad8a04887e5b2352"), pub);
        }

        [Fact]
        public void PubKeyToAddress()
        {
            byte[] pub = Hex.Decode("0250863ad64a87ae8a2fe83c1af1a8403cb53f53e486d8511dad8a04887e5b2352");

            byte[] sha256 = SHA256.Hash(pub);
            Assert.Equal(Hex.Decode("0b7c28c9b7290c98d7438e70b3d3f7c848fbd7d1dc194ff83f4f7cc9b1378e98"), sha256);

            byte[] ripemd160 = RIPEMD160.Hash(sha256);
            Assert.Equal(Hex.Decode("f54a5851e9372b87810a8e60cdd2e7cfd80b6e31"), ripemd160);

            byte[] ripemd160WithVersionByte = Hex.Decode($"00{Hex.ToHexString(ripemd160)}");
            Assert.Equal(Hex.Decode("00f54a5851e9372b87810a8e60cdd2e7cfd80b6e31"), ripemd160WithVersionByte);

            byte[] sha256_ex = SHA256.Hash(ripemd160WithVersionByte);
            Assert.Equal(Hex.Decode("ad3c854da227c7e99c4abfad4ea41d71311160df2e415e713318c70d67c6b41c"), sha256_ex);

            byte[] sha256_ex2 = SHA256.Hash(sha256_ex);
            Assert.Equal(Hex.Decode("c7f18fe8fcbed6396741e58ad259b5cb16b7fd7f041904147ba1dcffabf747fd"), sha256_ex2);

            byte[] checksum = sha256_ex2[..4];
            Assert.Equal(Hex.Decode("c7f18fe8"), checksum);

            byte[] binary_address =
                Hex.Decode($"{Hex.ToHexString(ripemd160WithVersionByte)}{Hex.ToHexString(checksum)}");
            Assert.Equal(Hex.Decode("00f54a5851e9372b87810a8e60cdd2e7cfd80b6e31c7f18fe8"), binary_address);

            string address = Base58.Encode(binary_address);
            Assert.Equal("1PMycacnJaSqwwJqjawXBErnLsZ7RkXUAs", address);
        }
    }
}
