using System;
using Org.BouncyCastle.Math;

namespace TinyCoin
{
    public static class Base58
    {
        private const string symbols = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static byte[] Decode(string base58)
        {
            var bi2 = new BigInteger("0");

            foreach (char c in base58)
                if (symbols.IndexOf(c) != -1)
                {
                    bi2 = bi2.Multiply(new BigInteger("58"));
                    bi2 = bi2.Add(new BigInteger(symbols.IndexOf(c).ToString()));
                }
                else
                {
                    return null;
                }

            byte[] bb = bi2.ToByteArrayUnsigned();

            foreach (char c in base58)
            {
                if (c != '1')
                    break;
                byte[] bbb = new byte[bb.Length + 1];
                Array.Copy(bb, 0, bbb, 1, bb.Length);
                bb = bbb;
            }

            return bb;
        }

        public static string Encode(byte[] ba)
        {
            var addrRemain = new BigInteger(1, ba);

            var big0 = new BigInteger("0");
            var big58 = new BigInteger("58");

            string rv = string.Empty;

            while (addrRemain.CompareTo(big0) > 0)
            {
                int d = Convert.ToInt32(addrRemain.Mod(big58).ToString());
                addrRemain = addrRemain.Divide(big58);
                rv = $"{symbols.Substring(d, 1)}{rv}";
            }

            foreach (byte b in ba)
            {
                if (b != 0)
                    break;
                rv = $"1{rv}";
            }

            return rv;
        }
    }
}
