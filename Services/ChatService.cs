using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Chat;

namespace ArqanumCore.Services
{
    public class ChatService(
        ChatStorage chatStorage,
        MessageStorage messageStorage,
        ContactStorage contactStorage)
    {
        public async Task<List<ChatPreviewViewModel>> GetAllChats()
        {
            var chats = await chatStorage.GetAllChatsAsync();
            var result = new List<ChatPreviewViewModel>();

            foreach (var chat in chats)
            {
                var lastMessage = await messageStorage.GetLastMessageAsync(chat.ChatId);
                var contact = await contactStorage.GetContactAsync(chat.ContactId);

                var preview = new ChatPreviewViewModel
                {
                    ChatId = chat.ChatId,
                    ChatName = (contact?.FirstName + contact?.LastName) ?? contact.Username,
                    LastMessage = lastMessage?.Content ?? string.Empty,
                    LastMessageTimestamp = lastMessage?.Timestamp ?? DateTime.MinValue,
                    UnreadMessagesCount = await messageStorage.GetCountUnreadMessages(chat.ChatId),
                    IsPinned = chat.IsPinned,
                };

                result.Add(preview);
            }

            return [.. result
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.LastMessageTimestamp)];
        }
    }
}
