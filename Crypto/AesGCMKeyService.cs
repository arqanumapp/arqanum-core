using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace ArqanumCore.Crypto
{
    public class AesGCMKeyService
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private static readonly SecureRandom SecureRng = new();

        public byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            byte[] nonce = new byte[NonceSize];
            SecureRng.NextBytes(nonce);

            byte[] ciphertext = new byte[plaintext.Length + TagSize];

            var cipher = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), TagSize * 8, nonce);

            cipher.Init(true, parameters);
            int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);
            cipher.DoFinal(ciphertext, len);

            byte[] result = new byte[NonceSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);

            return result;
        }

        public byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            byte[] nonce = new byte[NonceSize];
            byte[] ciphertext = new byte[encryptedData.Length - NonceSize];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, NonceSize, ciphertext, 0, ciphertext.Length);

            var cipher = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), TagSize * 8, nonce);

            cipher.Init(false, parameters);
            byte[] plaintext = new byte[ciphertext.Length];
            int len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
            cipher.DoFinal(plaintext, len);

            return plaintext;
        }
    }
}
