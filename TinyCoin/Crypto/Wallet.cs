namespace TinyCoin.Crypto;

public static class Wallet
{
    private const char PubKeyHashVersion = '1';

    public static string PubKeyToAddress(byte[] pubKey)
    {
        byte[] sha256 = SHA256.HashBinary(pubKey);
        byte[] ripemd160 = RIPEMD160.HashBinary(sha256);
        byte[] ripemd160WithVersionByte = Utils.HexStringToByteArray($"00{Utils.ByteArrayToHexString(ripemd160)}");
        byte[] sha256d = SHA256.DoubleHashBinary(ripemd160WithVersionByte);
        byte[] checksum = sha256d[..4];
        byte[] binaryAddress =
            Utils.HexStringToByteArray(
                $"{Utils.ByteArrayToHexString(ripemd160WithVersionByte)}{Utils.ByteArrayToHexString(checksum)}");
        string address = Base58.Encode(binaryAddress);
        return $"{PubKeyHashVersion}{address}";
    }
}
