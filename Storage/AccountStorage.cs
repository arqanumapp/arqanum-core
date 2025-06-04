using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;

namespace ArqanumCore.Storage
{
    public class AccountStorage(IDbPasswordProvider passwordProvider) : BaseStorage<Account>(passwordProvider)
    {
        public async Task<bool> SaveAccountAsync(Account account)
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _database.InsertAsync(account);
                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<Account?> GetAccountAsync()
        {
            try
            {
                await EnsureInitializedAsync();
                var account = await _database.Table<Account>().FirstOrDefaultAsync();
                return account;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
