using System.Globalization;
using System.Numerics;

namespace TinyCoin.Crypto;

public static class HashChecker
{
    public static bool IsValid(string hash, BigInteger targetHash)
    {
        var hashValue = BigInteger.Parse($"0x{hash}", NumberStyles.HexNumber);
        return hashValue < targetHash;
    }
}
