using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Account;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Account;
using System.Globalization;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class AccountService(MLDsaKeyService mLDsaKeyService, ShakeHashService shakeHashService, ProofOfWorkService proofOfWorkService, 
        ICaptchaProvider captchaProvider, ApiService apiService, AccountStorage accountStorage, SessionKeyStore sessionKeyStore)
    {

        public AccountViewModel CurrentAccount { get; } = new AccountViewModel();

        public async Task<bool> CreateAccount(string username, string? firstName = null, string? lastName = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
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

                var response = await apiService.PostSignBytesAsync(newAccountDto, privateKey, "account/register");

                cancellationToken.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    var avatarUrl = await response.Content.ReadAsStringAsync();
                    progress?.Report($"Registration successful! Avatar: {avatarUrl}");

                    newAccount.AvatarUrl = avatarUrl;

                    if (!await accountStorage.SaveAccountAsync(newAccount))
                        throw new Exception("Error saving account");

                    LoadAccount(newAccount);
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
            var requestDto = new UsernameAvailabilityRequestDto { Username = username, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

            var response = await apiService.PostJsonAsync(requestDto, "account/username-available");

            if (!response.IsSuccessStatusCode)
                return false;

            using var stream = await response.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var responseBody = await JsonSerializer.DeserializeAsync<UsernameAvailabilityResponseDto>(stream, options);

            if(responseBody.Available && TimestampValidator.IsValid(responseBody.Timestamp))
            {
                return true;
            }

            else return false;
        }

        public async Task<bool> AccountExist()
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is not null)
            {
                LoadAccount(account);
                return true;
            }
            return false;
        }

        private void LoadAccount(Account account)
        {
            sessionKeyStore.SetPrivateKey(account.SignaturePrivateKey);

            sessionKeyStore.SetAccountId(account.AccountId);

            CurrentAccount.AvatarUrl = account.AvatarUrl;
            CurrentAccount.Username = account.Username;
            CurrentAccount.FirstName = account.FirstName;
            CurrentAccount.LastName = account.LastName;
            CurrentAccount.AccountId = account.AccountId;
            CurrentAccount.Bio = account.Bio;
        }

        #region Update Account Methods

        public async Task<bool> UpdateFullNameAsync(string firstName, string lastName)
        {
            var updateFullNameRequestDto = new UpdateFullNameRequestDto
            {
                AccountId = sessionKeyStore.GetId() ?? throw new ArgumentNullException(nameof(sessionKeyStore.GetId), "Session key store does not contain account ID."),
                FirstName = firstName.Trim() ?? string.Empty,
                LastName = lastName.Trim() ?? string.Empty,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await apiService.PostSignBytesAsync(updateFullNameRequestDto, sPrK: sessionKeyStore.GetPrivateKey(), "account/update-fullname");

            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseBody = await JsonSerializer.DeserializeAsync<UpdateFullNameResponseDto>(stream, options);

                if (TimestampValidator.IsValid(responseBody.Timestamp))
                {
                    var account = await accountStorage.GetAccountAsync();

                    account.FirstName = firstName;
                    account.LastName = lastName;
                    account.Version = responseBody.Version;

                    await accountStorage.UpdateAccountAsync(account);

                    CurrentAccount.FirstName = firstName;
                    CurrentAccount.LastName = lastName;

                    return true;
                }

            }
            return false;
        }

        public async Task<bool> UpdateUsernameAsync(string username)
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is null)
                throw new Exception("Account not found");

            account.Username = username;
            return await accountStorage.SaveAccountAsync(account);
        }

        public async Task<bool> UpdateBioAsync(string bio)
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is null)
                throw new Exception("Account not found");

            account.Bio = bio;
            return await accountStorage.SaveAccountAsync(account);
        }

        public async Task<bool> UpdateAvatarAsync(string avatarUrl)
        {
            var account = await accountStorage.GetAccountAsync();
            if (account is null)
                throw new Exception("Account not found");
            account.AvatarUrl = avatarUrl;
            return await accountStorage.SaveAccountAsync(account);
        }

        #endregion
    }
}
