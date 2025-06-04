using SQLite;

namespace ArqanumCore.InternalModels
{
    [Table("Messages")]
    public class Message
    {
        [PrimaryKey, NotNull] public string MessageId { get; set; }
        [NotNull] public string ChatId { get; set; }
        [NotNull] public DateTime Timestamp { get; set; }
        [NotNull, MaxLength(1000)] public string Content { get; set; }
        public bool IsRead { get; set; }
        public bool FromSelf { get; set; }
    }
}
