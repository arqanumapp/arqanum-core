using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Contact;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using MessagePack;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class ContactService(ContactStorage contactStorage, ApiService apiService, SessionKeyStore sessionKeyStore, MLKemKeyService mLKemKeyService, MLDsaKeyService mLDsaKeyService)
    {
        //public async Task<GetContactViewModel?> GetContactAsync(string contactId)
        //{
        //    try
        //    {
        //        var contact = await contactStorage.GetContactAsync(contactId);

        //        return new GetContactViewModel
        //        {
        //            ContactId = contact.ContactId,
        //            Username = contact.Username,
        //            FirstName = contact?.FirstName,
        //            LastName = contact?.LastName,
        //            Bio = contact?.Bio,
        //            IsConfirmed = contact.SignaturePublicKey != null
        //        };
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public async Task<GetContactResponceDto?> FindContactAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || sessionKeyStore.GetId() == identifier || sessionKeyStore.GetUsername() == identifier)
            {
                return null;
            }

            var payload = new GetContactRequestDto { ContactIdentifier = identifier, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), AccountId = sessionKeyStore.GetId() };

            using var response = await apiService.PostSignBytesAsync(payload, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/find-contact");

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
                contact.Status = ContactStatus.Request;

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

                var response = await apiService.PostSignBytesAsync(request, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/add-contact");

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

        //public async Task<List<GetContactResponceDto>> GetContactsListAsync()
        //{
        //    try
        //    {
        //        var contacts = await contactStorage.GetAllContactsAsync();
        //        return [.. contacts.Where(c => c.SignaturePublicKey != null).Select(c => new GetContactViewModel
        //        {
        //            ContactId = c.ContactId,
        //            Username = c.Username,
        //            FirstName = c.FirstName,
        //            LastName = c.LastName,
        //            Bio = c.Bio,
        //            IsConfirmed = true,
        //        })];
        //    }
        //    catch
        //    {
        //        return [];
        //    }
        //}

        //public async Task<List<GetContactViewModel>> GetAllRequestContactsAsync()
        //{
        //    try
        //    {
        //        var contacts = await contactStorage.GetAllContactsAsync();
        //        return [.. contacts.Where(c => c.SignaturePublicKey == null).Select(c => new GetContactViewModel
        //        {
        //            ContactId = c.ContactId,
        //            Username = c.Username,
        //            FirstName = c.FirstName,
        //            LastName = c.LastName,
        //            Bio = c.Bio,
        //            IsConfirmed = false
        //        })];
        //    }
        //    catch (Exception ex)
        //    {
        //        return [];
        //    }
        //}

        //public async Task<bool> DeleteContactAsync(string contactId)
        //{
        //    try
        //    {
        //        return await contactStorage.DeleteContactAsync(contactId);
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
    }
}
