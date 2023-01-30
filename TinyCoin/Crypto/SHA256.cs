using Org.BouncyCastle.Crypto.Digests;

namespace TinyCoin.Crypto
{
    public static class SHA256
    {
        public static byte[] HashBinary(byte[] buffer)
        {
            var sha256 = new Sha256Digest();
            sha256.BlockUpdate(buffer, 0, buffer.Length);
            byte[] comparisonBytes = new byte[sha256.GetDigestSize()];
            sha256.DoFinal(comparisonBytes, 0);
            return comparisonBytes;
        }

        public static byte[] DoubleHashBinary(byte[] buffer)
        {
            return HashBinary(HashBinary(buffer));
        }
    }
}
