using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Account;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;

namespace ArqanumCore.Services
{
    public class AccountService
        (MLDsaKeyService mLDsaKeyService,
        ShakeHashService shakeHashService,
        ProofOfWorkService proofOfWorkService,
        ICaptchaProvider captchaProvider,
        ApiService apiService,
        AccountStorage accountStorage)
    {
        public async Task<bool> CreateAccount(string username, string? firstName = null, string? lastName = null, IProgress<string>? progress = null)
        {
            try
            {
                var newAccount = new Account();

                var (publicKey, privateKey) = mLDsaKeyService.GenerateKey();

                newAccount.Username = username;
                newAccount.FirstName = firstName;
                newAccount.LastName = lastName;
                newAccount.SignaturePublicKey = publicKey.GetEncoded();
                newAccount.SignaturePrivateKey = privateKey.GetEncoded();
                newAccount.AccountId = Convert.ToBase64String(shakeHashService.ComputeHash256(newAccount.SignaturePublicKey));

                var proofOfWork = proofOfWorkService.FindProof(Convert.ToBase64String(newAccount.SignaturePublicKey));

                var captchaToken = await captchaProvider.GetCaptchaTokenAsync();

                var newAccountDto = new CreateAccountDto
                {
                    AccountId = newAccount.AccountId,
                    Username = newAccount.Username,
                    FirstName = newAccount.FirstName,
                    LastName = newAccount.LastName,
                    SignaturePublicKey = newAccount.SignaturePublicKey,
                    ProofOfWork = proofOfWork.hash,
                    ProofOfWorkNonce = proofOfWork.nonce,
                    ChaptchaToken = string.Empty,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var responce = await apiService.PostAsync(newAccountDto, privateKey, "account/register");

                if (responce.IsSuccessStatusCode)
                {
                    progress?.Report("Registration successful!");
                    if (!await accountStorage.SaveAccountAsync(newAccount))
                    {
                        throw new Exception("Error saving account");
                    }
                }
                return responce.IsSuccessStatusCode;
            }
            catch
            {
                progress?.Report($"Error creating account");
                return false;
            }
        }

        public async Task<bool> AccountExist()
        {
            if (await accountStorage.GetAccountAsync() is not null)
            {
                return true;
            }
            return false;
        }

        public async Task<Account?> GetAccountAsync()
        {
            return await accountStorage.GetAccountAsync();
        }
    }
}
