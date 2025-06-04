using ArqanumCore.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace ArqanumCore.Services
{
    public class SessionKeyStore(MLDsaKeyService mLDsaKeyService)
    {
        private MLDsaPrivateKeyParameters? _privateKey;
        private readonly object _lock = new();

        public MLDsaPrivateKeyParameters? GetPrivateKey()
        {
            lock (_lock)
            {
                return _privateKey;
            }
        }

        public void SetPrivateKey(byte[] privateKey)
        {
            lock (_lock)
            {
                _privateKey = mLDsaKeyService.RecoverPrivateKey([.. privateKey]);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _privateKey = null;
            }
        }

        public bool IsLoaded
        {
            get
            {
                lock (_lock) return _privateKey != null;
            }
        }
    }
}
