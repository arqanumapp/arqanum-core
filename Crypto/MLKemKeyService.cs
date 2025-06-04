using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace ArqanumCore.Crypto
{
    public class MLKemKeyService
    {
        private static readonly SecureRandom Random = new();

        public (MLKemPublicKeyParameters publicKey, MLKemPrivateKeyParameters privateKey) GenerateKey()
        {
            var kemKpg = new MLKemKeyPairGenerator();
            kemKpg.Init(new MLKemKeyGenerationParameters(Random, MLKemParameters.ml_kem_1024));
            var kemKp = kemKpg.GenerateKeyPair();
            return ((MLKemPublicKeyParameters)kemKp.Public, (MLKemPrivateKeyParameters)kemKp.Private);
        }

        public (byte[] kemCipherText, byte[] sharedSecret) Encapsulate(byte[] receiverPublicKeyBytes)
        {
            var publicKey = PublicKeyFactory.CreateKey(receiverPublicKeyBytes);
            var encapsulator = KemUtilities.GetEncapsulator("ML-KEM-1024");
            encapsulator.Init(publicKey);
            var kemCipherText = new byte[encapsulator.EncapsulationLength];
            var sharedSecret = new byte[encapsulator.SecretLength];
            encapsulator.Encapsulate(kemCipherText, 0, kemCipherText.Length, sharedSecret, 0, sharedSecret.Length);
            return (kemCipherText, sharedSecret);
        }

        public byte[] Decapsulate(byte[] kemCipherText, byte[] privateKey)
        {
            var privateKeyObj = MLKemPrivateKeyParameters.FromEncoding(MLKemParameters.ml_kem_1024, privateKey);
            var decapsulator = KemUtilities.GetDecapsulator("ML-KEM-1024");
            decapsulator.Init(privateKeyObj);
            var sharedSecret = new byte[decapsulator.SecretLength];
            decapsulator.Decapsulate(kemCipherText, 0, kemCipherText.Length, sharedSecret, 0, sharedSecret.Length);
            return sharedSecret;
        }
    }
}
