using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;

namespace ArqanumCore.Storage
{
    public class ContactStorage(IDbPasswordProvider passwordProvider) : BaseStorage<Contact>(passwordProvider)
    {
        public async Task<bool> SaveContactAsync(Contact contact)
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _database.InsertAsync(contact);
                return result > 0;
            }
            catch 
            {
                return false;
            }
        }
        public async Task<Contact?> GetContactAsync(string contactId)
        {
            try
            {
                await EnsureInitializedAsync();
                var contact = await _database.Table<Contact>().Where(c => c.ContactId == contactId).FirstOrDefaultAsync();
                return contact;
            }
            catch 
            {
                return null;
            }
        }
        public async Task<bool> DeleteContactAsync(string contactId)
        {
            try
            {
                await EnsureInitializedAsync();
                var contact = await GetContactAsync(contactId);
                if (contact != null)
                {
                    await _database.DeleteAsync(contact);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Contact>> GetAllContactsAsync()
        {
            try
            {
                await EnsureInitializedAsync();
                var contacts = await _database.Table<Contact>().ToListAsync();
                return contacts;
            }
            catch
            {
                return [];
            }
        }
    }
}
