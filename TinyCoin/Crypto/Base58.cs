using System;
using Org.BouncyCastle.Math;

namespace TinyCoin.Crypto
{
    public static class Base58
    {
        private const string Symbols = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        private static readonly BigInteger BI58 = new BigInteger("58");
        private static readonly BigInteger BI0 = new BigInteger("0");

        public static byte[] Decode(string base58)
        {
            var bi2 = new BigInteger("0");

            foreach (char c in base58)
            {
                int index = Symbols.IndexOf(c);
                if (index != -1)
                {
                    bi2 = bi2.Multiply(BI58);
                    bi2 = bi2.Add(new BigInteger(index.ToString()));
                }
                else
                {
                    return null;
                }
            }

            return bi2.ToByteArray();
        }

        public static string Encode(byte[] buffer)
        {
            var addrRemain = new BigInteger(1, buffer);

            string rv = string.Empty;

            while (addrRemain.CompareTo(BI0) > 0)
            {
                int d = Convert.ToInt32(addrRemain.Mod(BI58).ToString());
                addrRemain = addrRemain.Divide(BI58);
                rv = $"{Symbols[d]}{rv}";
            }

            return rv;
        }
    }
}
