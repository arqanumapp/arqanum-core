using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;

namespace ArqanumCore.Storage
{
    public class MessageStorage(IDbPasswordProvider passwordProvider) : BaseStorage<Message>(passwordProvider)
    {
        public async Task<bool> SaveMessageAsync(Message message)
        {
            try
            {
                await EnsureInitializedAsync();
                var result = await _database.InsertAsync(message);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }
        public async Task<Message?> GetLastMessageAsync(string chatId)
        {
            try
            {
                await EnsureInitializedAsync();
                var message = await _database.Table<Message>().Where(m => m.ChatId == chatId).FirstOrDefaultAsync();
                return message;
            }
            catch
            {
                return null;
            }
        }
        private async Task<Message?> GetMessageAsync(string messageId)
        {
            try
            {
                await EnsureInitializedAsync();
                var message = await _database.Table<Message>().Where(m => m.MessageId == messageId).FirstOrDefaultAsync();
                return message;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId)
        {
            try
            {
                await EnsureInitializedAsync();
                var message = await GetMessageAsync(messageId);
                if (message != null)
                {
                    await _database.DeleteAsync(message);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<Message>> GetAllMessagesAsync(string chatId)
        {
            try
            {
                await EnsureInitializedAsync();
                var messages = await _database.Table<Message>().Where(m => m.ChatId == chatId).ToListAsync();
                return messages;
            }
            catch
            {
                return [];
            }
        }

        public async Task<int> GetCountUnreadMessages(string chatId)
        {
            try
            {
                await EnsureInitializedAsync();
                var count = await _database.Table<Message>()
                    .Where(m => m.ChatId == chatId && !m.IsRead)
                    .CountAsync();
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
