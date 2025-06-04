using Org.BouncyCastle.Crypto.Digests;
using System.Text;

namespace ArqanumCore.Crypto
{
    public class ShakeHashService
    {
        public byte[] ComputeHash256(byte[] input, int outputLength = 32)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputLength);
            var digest = new ShakeDigest(256);
            digest.BlockUpdate(input, 0, input.Length);
            var output = new byte[outputLength];
            digest.DoFinal(output, 0);
            return output;
        }
        public byte[] ComputeHash128(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            var shake = new ShakeDigest(128);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            shake.BlockUpdate(inputBytes, 0, inputBytes.Length);
            var output = new byte[32];
            shake.DoFinal(output, 0);
            return output;
        }
    }
}
