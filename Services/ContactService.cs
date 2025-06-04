using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Contact;

namespace ArqanumCore.Services
{
    public class ContactService(ContactStorage contactStorage)
    {
        public async Task<GetContactViewModel?> GetContactAsync(string contactId)
        {
            try
            {
                var contact = await contactStorage.GetContactAsync(contactId);

                return new GetContactViewModel
                {
                    ContactId = contact.ContactId,
                    Username = contact.Username,
                    FirstName = contact?.FirstName,
                    LastName = contact?.LastName,
                    Bio = contact?.Bio,
                    IsConfirmed = contact.SignaturePublicKey != null
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<GetContactViewModel> FindContactAsync(string username)
        {
            return new();
        }

        public async Task<bool> AddContactAsync(string ContactId)
        {
            return true;
        }

        public async Task<List<GetContactViewModel>> GetContactsListAsync()
        {
            try
            {
                var contacts = await contactStorage.GetAllContactsAsync();
                return [.. contacts.Where(c => c.SignaturePublicKey != null).Select(c => new GetContactViewModel
                {
                    ContactId = c.ContactId,
                    Username = c.Username,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Bio = c.Bio,
                    IsConfirmed = true,
                })];
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<GetContactViewModel>> GetAllRequestContactsAsync()
        {
            try
            {
                var contacts = await contactStorage.GetAllContactsAsync();
                return [.. contacts.Where(c => c.SignaturePublicKey == null).Select(c => new GetContactViewModel
                {
                    ContactId = c.ContactId,
                    Username = c.Username,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Bio = c.Bio,
                    IsConfirmed = false
                })];
            }
            catch (Exception ex)
            {
                return [];
            }
        }

        public async Task<bool> DeleteContactAsync(string contactId)
        {
            try
            {
                return await contactStorage.DeleteContactAsync(contactId);
            }
            catch
            {
                return false;
            }
        }
    }
}
