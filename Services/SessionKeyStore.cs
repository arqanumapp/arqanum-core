using ArqanumCore.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace ArqanumCore.Services
{
    public class SessionKeyStore(MLDsaKeyService mLDsaKeyService)
    {
        private MLDsaPrivateKeyParameters? _privateKey;

        private string _accountId;

        private string _username;

        private readonly object _lock = new();

        public MLDsaPrivateKeyParameters GetPrivateKey()
        {
            lock (_lock)
            {
                return _privateKey;
            }
        }

        public void SetUsername(string username)
        {
            lock (_lock)
            {
                _username = username;
            }
        }

        public string GetUsername()
        {
            lock (_lock)
            {
                return _username;
            }
        }

        public string GetId()
        {
            lock (_lock)
            {
                return _accountId;
            }
        }

        public void SetPrivateKey(byte[] privateKey)
        {
            lock (_lock)
            {
                _privateKey = mLDsaKeyService.RecoverPrivateKey([.. privateKey]);
            }
        }

        public void SetAccountId(string accountId)
        {
            lock (_lock)
            {
                _accountId = accountId;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _privateKey = null;
                _accountId = string.Empty;
            }
        }

        public bool IsLoaded
        {
            get
            {
                lock (_lock) return _privateKey != null && !string.IsNullOrEmpty(_accountId);
            }
        }
    }
}
