﻿using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Account;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Account;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class AccountService(MLDsaKeyService mLDsaKeyService, ShakeHashService shakeHashService, ProofOfWorkService proofOfWorkService,
        ICaptchaProvider captchaProvider, ApiService apiService, AccountStorage accountStorage, SessionKeyStore sessionKeyStore, ISignalRClientService signalRClientService, IFileCacheService fileCacheService)
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

                if (response is null)
                {
                    progress?.Report("Error connecting to server.");
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    var avatarUrl = await response.Content.ReadAsStringAsync();

                    var fileName = fileCacheService.GetFileNameFromUrl(avatarUrl);
                    var cachedAvatarPath = await fileCacheService.GetOrDownloadFilePathAsync(avatarUrl, fileName);

                    newAccount.AvatarUrl = cachedAvatarPath;

                    progress?.Report($"Registration successful!");

                    if (!await accountStorage.SaveAccountAsync(newAccount))
                        throw new Exception("Error saving account");

                    progress?.Report($"Loading...");

                    await LoadAccount(newAccount);
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

            if (response is null || !response.IsSuccessStatusCode)
                return false;

            using var stream = await response.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var responseBody = await JsonSerializer.DeserializeAsync<UsernameAvailabilityResponseDto>(stream, options);

            if (responseBody.Available && TimestampValidator.IsValid(responseBody.Timestamp))
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
                await LoadAccount(account);
                return true;
            }
            return false;
        }

        private async Task LoadAccount(Account account)
        {
            sessionKeyStore.SetPrivateKey(account.SignaturePrivateKey);

            sessionKeyStore.SetAccountId(account.AccountId);

            sessionKeyStore.SetUsername(account.Username);

            sessionKeyStore.SetPublicKey(account.SignaturePublicKey);

            CurrentAccount.AvatarUrl = $"file:///{account.AvatarUrl.Replace("\\", "/")}";
            CurrentAccount.Username = account.Username;
            CurrentAccount.FirstName = account.FirstName;
            CurrentAccount.LastName = account.LastName;
            CurrentAccount.AccountId = account.AccountId;
            CurrentAccount.Bio = account.Bio;

            await signalRClientService.StartAsync();
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

            if (response != null && response.IsSuccessStatusCode)
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

                    account.FirstName = updateFullNameRequestDto.FirstName;
                    account.LastName = updateFullNameRequestDto.LastName;
                    account.Version = responseBody.Version;

                    await accountStorage.UpdateAccountAsync(account);

                    CurrentAccount.FirstName = updateFullNameRequestDto.FirstName;
                    CurrentAccount.LastName = updateFullNameRequestDto.LastName;

                    return true;
                }
            }
            return false;
        }

        public async Task<bool> UpdateUsernameAsync(string username)
        {
            var updateUsernameRequestDto = new UpdateUsernameRequestDto
            {
                AccountId = sessionKeyStore.GetId(),
                NewUsername = username.Trim(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await apiService.PostSignBytesAsync(updateUsernameRequestDto, sPrK: sessionKeyStore.GetPrivateKey(), "account/update-username");

            if (response != null && response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseBody = await JsonSerializer.DeserializeAsync<UpdateUsernameResponseDto>(stream, options);

                if (TimestampValidator.IsValid(responseBody.Timestamp))
                {
                    var account = await accountStorage.GetAccountAsync();

                    account.Username = updateUsernameRequestDto.NewUsername;
                    account.Version = responseBody.Version;

                    await accountStorage.UpdateAccountAsync(account);

                    CurrentAccount.Username = updateUsernameRequestDto.NewUsername;
                    await accountStorage.SaveAccountAsync(account);
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> UpdateBioAsync(string bio)
        {
            var updateBioRequestDto = new UpdateBioRequestDto
            {
                AccountId = sessionKeyStore.GetId(),
                Bio = bio,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await apiService.PostSignBytesAsync(updateBioRequestDto, sPrK: sessionKeyStore.GetPrivateKey(), "account/update-bio");

            if (response != null && response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseBody = await JsonSerializer.DeserializeAsync<UpdateBioResponseDto>(stream, options);

                if (TimestampValidator.IsValid(responseBody.Timestamp))
                {
                    var account = await accountStorage.GetAccountAsync();


                    account.Version = responseBody.Version;
                    account.Bio = updateBioRequestDto.Bio;

                    await accountStorage.UpdateAccountAsync(account);

                    CurrentAccount.Bio = updateBioRequestDto.Bio;

                    return true;
                }
            }
            return false;
        }

        public async Task<bool> UpdateAvatarAsync(byte[] imageData, string format)
        {
            var updateAvatarRequestDto = new UpdateAvatarRequestDto
            {
                AccountId = sessionKeyStore.GetId(),
                AvatarData = imageData,
                Format = format.ToLowerInvariant(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await apiService.PostSignBytesAsync(updateAvatarRequestDto, sPrK: sessionKeyStore.GetPrivateKey(), "account/update-avatar");

            if (response != null && response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseBody = await JsonSerializer.DeserializeAsync<UpdateAvatarResponseDto>(stream, options);

                if (TimestampValidator.IsValid(responseBody.Timestamp))
                {
                    var account = await accountStorage.GetAccountAsync();

                    account.Version = responseBody.Version;

                    string fileName = Path.GetFileName(account.AvatarUrl);

                    var res = await fileCacheService.DeleteCachedFileAsync(fileName);

                    var newFileName = fileCacheService.GetFileNameFromUrl(responseBody.AvatarUrl);

                    var cachedAvatarPath = await fileCacheService.GetOrDownloadFilePathAsync(responseBody.AvatarUrl, newFileName);

                    account.AvatarUrl = cachedAvatarPath;

                    await accountStorage.UpdateAccountAsync(account);

                    CurrentAccount.AvatarUrl = account.AvatarUrl;

                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
