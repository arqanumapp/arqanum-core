namespace ArqanumCore.ViewModels.Chat
{
    public class ChatPreviewViewModel
    {
        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public DateTime LastMessageTimestamp { get; set; }
        public int UnreadMessagesCount { get; set; }
        public string LastMessage { get; set; }
        public bool IsPinned { get; set; }
    }
}
