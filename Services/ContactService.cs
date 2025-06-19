using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Contact;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using MessagePack;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class ContactService(ContactStorage contactStorage, ApiService apiService, SessionKeyStore sessionKeyStore, MLKemKeyService mLKemKeyService, MLDsaKeyService mLDsaKeyService, ShakeHashService shakeHashService, IShowNotyficationService showNotyficationService)
    {
        public async Task<GetContactResponceDto?> FindContactAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || sessionKeyStore.GetId() == identifier || sessionKeyStore.GetUsername() == identifier)
            {
                return null;
            }

            var payload = new GetContactRequestDto { ContactIdentifier = identifier, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), AccountId = sessionKeyStore.GetId() };

            using var response = await apiService.PostSignBytesAsync(payload, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/find");

            if (response is null || !response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            var contact = JsonSerializer.Deserialize<GetContactResponceDto>(json, options);

            if (!TimestampValidator.IsValid(contact.Timestamp))
                throw new InvalidOperationException("Invalid timestamp in contact response");

            return contact;
        }

        public async Task<bool> AddContactAsync(GetContactResponceDto getContactResponceDto)
        {
            try
            {
                var contact = new Contact();
                contact.ContactId = getContactResponceDto.ContactId;
                contact.Username = getContactResponceDto.Username;
                contact.AvatarUrl = getContactResponceDto.AvatarUrl;
                contact.FirstName = getContactResponceDto.FirstName;
                contact.LastName = getContactResponceDto.LastName;
                contact.Bio = getContactResponceDto.Bio;
                contact.Version = getContactResponceDto.Version;
                contact.Status = ContactStatus.Pending;

                var (PublicKey, PrivateKey) = mLKemKeyService.GenerateKey();

                contact.MyPublicKey = PublicKey.GetEncoded();
                contact.MyPrivateKey = PrivateKey.GetEncoded();

                var payload = new ContactPayload
                {
                    SenderId = sessionKeyStore.GetId(),
                    ContactPublicKey = contact.MyPublicKey,
                    SignaturePublicKey = sessionKeyStore.GetPublicKey().GetEncoded(),
                };
                var payloadBytes = MessagePackSerializer.Serialize(payload);

                var payloadBytesSignature = mLDsaKeyService.Sign(payloadBytes, sessionKeyStore.GetPrivateKey());

                var request = new AddContactRequestDto
                {
                    RecipientId = contact.ContactId,
                    Payload = payloadBytes,
                    PayloadSignature = payloadBytesSignature,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };

                var response = await apiService.PostSignBytesAsync(request, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/add");

                if (response != null && response.IsSuccessStatusCode)
                {
                    await contactStorage.SaveContactAsync(contact);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task NewContactRequest(byte[] payload, byte[] payloadSignature)
        {
            try
            {
                var newContactMessage = MessagePackSerializer.Deserialize<ContactPayload>(payload);

                if (Convert.ToBase64String(shakeHashService.ComputeHash256(newContactMessage.SignaturePublicKey, 64)) != newContactMessage.SenderId)
                {
                    return;
                }

                if (!mLDsaKeyService.Verify(newContactMessage.SignaturePublicKey, payload, payloadSignature))
                {
                    return;
                }

                var contactInfo = await FindContactAsync(newContactMessage.SenderId);

                if (contactInfo == null)
                {
                    return;
                }

                var contact = new Contact
                {
                    ContactId = contactInfo.ContactId,
                    Username = contactInfo.Username,
                    AvatarUrl = contactInfo.AvatarUrl,
                    FirstName = contactInfo.FirstName,
                    LastName = contactInfo.LastName,
                    Bio = contactInfo.Bio,
                    Version = contactInfo.Version,
                    SignaturePublicKey = newContactMessage.SignaturePublicKey,
                    ContactPublicKey = newContactMessage.ContactPublicKey,
                    Status = ContactStatus.Request,
                };
                await contactStorage.SaveContactAsync(contact);
                showNotyficationService.ShowNotificationAsync("New contact request", "Username");
            }
            catch
            {
                return;
            }
        }
    }
}
