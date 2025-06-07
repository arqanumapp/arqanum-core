using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Account;
using ArqanumCore.Interfaces;
using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Account;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class AccountService
        (MLDsaKeyService mLDsaKeyService,
        ShakeHashService shakeHashService,
        ProofOfWorkService proofOfWorkService,
        ICaptchaProvider captchaProvider,
        ApiService apiService,
        AccountStorage accountStorage,
        SessionKeyStore sessionKeyStore)
    {
        public async Task<bool> CreateAccount(
            string username,
            string? firstName = null,
            string? lastName = null,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var newAccount = new InternalModels.Account();

                var (publicKey, privateKey) = mLDsaKeyService.GenerateKey();

                newAccount.Username = username;
                newAccount.FirstName = firstName;
                newAccount.LastName = lastName;
                newAccount.SignaturePublicKey = publicKey.GetEncoded();
                newAccount.SignaturePrivateKey = privateKey.GetEncoded();
                newAccount.AccountId = Convert.ToBase64String(shakeHashService.ComputeHash256(newAccount.SignaturePublicKey, 64));

                var proofOfWork = await proofOfWorkService.FindProofAsync(
                    Convert.ToBase64String(newAccount.SignaturePublicKey),
                    progress,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

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
                    CaptchaToken = captchaToken,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var response = await apiService.PostAsync(newAccountDto, privateKey, "account/register");

                cancellationToken.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    progress?.Report("Registration successful!");
                    if (!await accountStorage.SaveAccountAsync(newAccount))
                    {
                        throw new Exception("Error saving account");
                    }

                    LoadAccount(newAccount.SignaturePrivateKey);
                }

                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                progress?.Report("Account creation canceled.");
                return false;
            }
            catch (Exception)
            {
                progress?.Report("Error creating account.");
                return false;
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            var requestDto = new UsernameAvailabilityRequestDto { Username = username };
            var response = await apiService.PostJsonAsync(requestDto, "account/username-available");

            if (!response.IsSuccessStatusCode)
                return false;

            using var stream = await response.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var responseBody = await JsonSerializer.DeserializeAsync<UsernameAvailabilityResponseDto>(stream, options);

            return responseBody?.Available ?? false;
        }


        public async Task<bool> AccountExist()
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is not null)
            {
                LoadAccount(account.SignaturePrivateKey);
                return true;
            }
            return false;
        }

        private void LoadAccount(byte[] privateKey) => sessionKeyStore.SetPrivateKey(privateKey);

        public async Task<AccountViewModel> GetAccountAsync()
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is not null)
            {
                return new AccountViewModel
                {
                    Username = account.Username,
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    AccountId = account.AccountId,
                    Bio = account.Bio
                };
            }
            else
            {
                throw new Exception("Account not found");
            }
        }
    }
}
