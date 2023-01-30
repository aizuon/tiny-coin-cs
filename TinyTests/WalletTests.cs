using TinyCoin;
using Xunit;

namespace TinyTests
{
    public class WalletTests
    {
        [Fact]
        public void PubKeyToAddress()
        {
            byte[] pubKey =
                Utils.HexStringToByteArray("0250863ad64a87ae8a2fe83c1af1a8403cb53f53e486d8511dad8a04887e5b2352");

            string address = Wallet.PubKeyToAddress(pubKey);
            Assert.Equal("1PMycacnJaSqwwJqjawXBErnLsZ7RkXUAs", address);
        }
    }
}
