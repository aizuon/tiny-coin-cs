using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace TinyCoin
{
    public static class ECDSA
    {
        private static readonly X9ECParameters Curve = ECNamedCurveTable.GetByName("secp256k1");
        private static readonly ECDomainParameters DomainParams;

        static ECDSA()
        {
            DomainParams = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H, Curve.GetSeed());
        }

        public static (byte[], byte[]) Generate()
        {
            var generator = new ECKeyPairGenerator("ECDSA");
            generator.Init(new ECKeyGenerationParameters(DomainParams, new SecureRandom()));
            var keyPair = generator.GenerateKeyPair();

            var privateKey = keyPair.Private as ECPrivateKeyParameters;
            var publicKey = keyPair.Public as ECPublicKeyParameters;

            return (privateKey.D.ToByteArray(), publicKey.Q.GetEncoded(true));
        }

        public static byte[] GetPubKeyFromPrivKey(byte[] privKey)
        {
            var privKeyParams = new ECPrivateKeyParameters(new BigInteger(privKey), DomainParams);

            var publicKeyViaPrivate = privKeyParams.Parameters.G.Multiply(privKeyParams.D).Normalize();
            return publicKeyViaPrivate.GetEncoded(true);
        }

        public static byte[] SignMsg(byte[] msg, byte[] privKey)
        {
            var signer = SignerUtilities.GetSigner("ECDSAWITHNONE");
            signer.Init(true, new ECPrivateKeyParameters(new BigInteger(privKey), DomainParams));
            signer.BlockUpdate(msg, 0, msg.Length);
            return signer.GenerateSignature();
        }

        public static bool VerifySig(byte[] sig, byte[] msg, byte[] pubKey)
        {
            var signer = SignerUtilities.GetSigner("ECDSAWITHNONE");
            signer.Init(false, new ECPublicKeyParameters(new X9ECPoint(Curve.Curve, pubKey).Point, DomainParams));
            signer.BlockUpdate(msg, 0, msg.Length);
            return signer.VerifySignature(sig);
        }
    }
}
