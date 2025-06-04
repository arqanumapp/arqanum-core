using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace ArqanumCore.Crypto
{
    public class MLDsaKeyService
    {
        private static readonly SecureRandom Random = new();

        public (MLDsaPublicKeyParameters PublicKey, MLDsaPrivateKeyParameters PrivateKey) GenerateKey()
        {
            var keyGen = new MLDsaKeyPairGenerator();
            keyGen.Init(new MLDsaKeyGenerationParameters(Random, MLDsaParameters.ml_dsa_87));
            var keyPair = keyGen.GenerateKeyPair();
            return ((MLDsaPublicKeyParameters)keyPair.Public, (MLDsaPrivateKeyParameters)keyPair.Private);
        }

        public byte[] Sign(byte[] data, MLDsaPrivateKeyParameters privateKey)
        {
            var signer = new MLDsaSigner(MLDsaParameters.ml_dsa_87, true);
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public bool Verify(byte[] publicKeyBytes, byte[] message, byte[] signature)
        {
            var publicKey = MLDsaPublicKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_87, publicKeyBytes);
            var verifier = new MLDsaSigner(MLDsaParameters.ml_dsa_87, false);
            verifier.Init(false, publicKey);
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }

        public MLDsaPublicKeyParameters RecoverPublicKey(byte[] publicKeyBytes) => MLDsaPublicKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_87, publicKeyBytes);

        public MLDsaPrivateKeyParameters RecoverPrivateKey(byte[] privateKeyBytes) => MLDsaPrivateKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_87, privateKeyBytes);
    }
}
