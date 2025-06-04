using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;

namespace ArqanumCore.Storage
{
    public class ChatStorage(IDbPasswordProvider passwordProvider) : BaseStorage<Chat>(passwordProvider)
    {
        public async Task<bool> SaveChatAsync(Chat chat)
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _database.InsertAsync(chat);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Chat?> GetChatAsync(string chatId)
        {
            try
            {
                await EnsureInitializedAsync();
                var chat = await _database.Table<Chat>().Where(c => c.ChatId == chatId).FirstOrDefaultAsync();
                return chat;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteChatAsync(string chatId)
        {
            try
            {
                await EnsureInitializedAsync();
                var chat = await GetChatAsync(chatId);
                if (chat != null)
                {
                    await _database.DeleteAsync(chat);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<Chat>> GetAllChatsAsync()
        {
            try
            {
                await EnsureInitializedAsync();
                var chats = await _database.Table<Chat>().ToListAsync();
                return chats;
            }
            catch
            {
                return new List<Chat>();
            }
        }
    }
}
