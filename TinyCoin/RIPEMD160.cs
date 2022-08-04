using Org.BouncyCastle.Crypto.Digests;

namespace TinyCoin
{
    public static class RIPEMD160
    {
        public static byte[] Hash(byte[] data)
        {
            var sha256 = new RipeMD160Digest();
            sha256.BlockUpdate(data, 0, data.Length);
            byte[] comparisonBytes = new byte[sha256.GetDigestSize()];
            sha256.DoFinal(comparisonBytes, 0);
            return comparisonBytes;
        }
    }
}
