using ArqanumCore.Crypto;
using System.Security.Cryptography;

namespace ArqanumCore.Services
{
    public class ProofOfWorkService(ShakeHashService shakeGenerator)
    {
        public (string nonce, string hash) FindProof(string publicKey, IProgress<string>? progress = null)
        {
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[4];
            int attempts = 0;
            while (true)
            {
                rng.GetBytes(buffer);
                var nonce = BitConverter.ToUInt32(buffer, 0).ToString("X");
                string hash = Convert.ToBase64String(shakeGenerator.ComputeHash128(publicKey + nonce)).ToLowerInvariant();

                if (attempts % 100 == 0)
                    progress?.Report(("PoW: " + hash + nonce));

                if (hash.StartsWith("000"))
                    return (nonce, hash);

                attempts++;
                if (attempts > 10_000_000)
                    throw new Exception("PoW failed.");
            }
        }
    }
}
