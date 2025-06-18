using ArqanumCore.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace ArqanumCore.Services
{
    public class SessionKeyStore(MLDsaKeyService mLDsaKeyService)
    {
        private MLDsaPrivateKeyParameters? _privateKey;

        private MLDsaPublicKeyParameters? _publicKey;

        private string _accountId;

        private string _username;

        private readonly object _lock = new();

        #region Getters

        public MLDsaPrivateKeyParameters GetPrivateKey()
        {
            lock (_lock)
            {
                return _privateKey;
            }
        }
        public MLDsaPublicKeyParameters GetPublicKey()
        {
            lock (_lock)
            {
                return _publicKey;
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

        #endregion

        #region Setters

        public void SetUsername(string username)
        {
            lock (_lock)
            {
                _username = username;
            }
        }

        public void SetPublicKey(byte[] publicKey)
        {
            lock (_lock)
            {
                _publicKey = mLDsaKeyService.RecoverPublicKey(publicKey);
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

        #endregion

        public void Clear()
        {
            lock (_lock)
            {
                _privateKey = null;
                _accountId = string.Empty;
                _username = string.Empty;
                _publicKey = null;
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
