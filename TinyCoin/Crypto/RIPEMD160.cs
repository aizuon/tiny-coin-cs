using Org.BouncyCastle.Crypto.Digests;

namespace TinyCoin.Crypto;

public static class RIPEMD160
{
    public static byte[] HashBinary(byte[] buffer)
    {
        var sha256 = new RipeMD160Digest();
        sha256.BlockUpdate(buffer, 0, buffer.Length);
        byte[] comparisonBytes = new byte[sha256.GetDigestSize()];
        sha256.DoFinal(comparisonBytes, 0);
        return comparisonBytes;
    }
}
