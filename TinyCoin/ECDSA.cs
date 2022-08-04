using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace TinyCoin
{
    public static class ECDSA
    {
        private static BigInteger _two = new BigInteger("02");
        private static BigInteger _zero = new BigInteger("00");

        private static ECKeyGenerationParameters Get_SECP256k1()
        {
            var curve = ECNamedCurveTable.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

            var secureRandom = new SecureRandom();
            var keyParams = new ECKeyGenerationParameters(domainParams, secureRandom);

            return keyParams;
        }

        public static (byte[], byte[]) Generate()
        {
            var keyParams = Get_SECP256k1();

            var generator = new ECKeyPairGenerator("ECDSA");
            generator.Init(keyParams);
            var keyPair = generator.GenerateKeyPair();

            var privateKey = keyPair.Private as ECPrivateKeyParameters;
            var publicKey = keyPair.Public as ECPublicKeyParameters;

            return (privateKey.D.ToByteArrayUnsigned(), publicKey.Q.GetEncoded(true));
        }

        public static byte[] GetPubKeyFromPrivKey(byte[] privKey)
        {
            var keyParams = Get_SECP256k1();
            var privKeyParams = new ECPrivateKeyParameters(new BigInteger(privKey), keyParams.DomainParameters);

            var publicKeyViaPrivate = privKeyParams.Parameters.G.Multiply(privKeyParams.D).Normalize();
            return publicKeyViaPrivate.GetEncoded(true);
        }
    }
}
