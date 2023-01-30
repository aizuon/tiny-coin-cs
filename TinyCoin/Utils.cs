using Org.BouncyCastle.Utilities.Encoders;

namespace TinyCoin
{
    public static class Utils
    {
        public static string ByteArrayToHexString(byte[] arr)
        {
            return Hex.ToHexString(arr);
        }

        public static byte[] HexStringToByteArray(string str)
        {
            return Hex.Decode(str);
        }

        public static byte[] StringToByteArray(string str)
        {
            byte[] arr = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                arr[i] = (byte)str[i];

            return arr;
        }
    }
}
