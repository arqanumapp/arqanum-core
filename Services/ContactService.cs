using ArqanumCore.Dtos.Contact;
using ArqanumCore.Storage;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class ContactService(ContactStorage contactStorage, ApiService apiService, SessionKeyStore sessionKeyStore)
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
            var payload = new GetContactRequestDto { ContactIdentifier = identifier, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), AccountId = sessionKeyStore.GetId() };

            using var response = await apiService.PostSignBytesAsync(payload, sPrK: sessionKeyStore.GetPrivateKey()
                ?? throw new ArgumentNullException(), route: "contact/find-contact");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            var contact = JsonSerializer.Deserialize<GetContactResponceDto>(json, options);

            if (contact is null)
                throw new InvalidOperationException("Failed to deserialize contact response");

            return contact;
        }


        public async Task<bool> AddContactAsync(string ContactId)
        {
            return true;
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
