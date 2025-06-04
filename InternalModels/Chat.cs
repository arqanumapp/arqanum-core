using SQLite;

namespace ArqanumCore.InternalModels
{
    [Table("Chats")]
    public class Chat
    {
        [PrimaryKey, NotNull] public string ChatId { get; set; }

        [NotNull] public string ContactId { get; set; }

        [NotNull] public byte[] PublicKey { get; set; }

        [NotNull] public byte[] PrivateKey { get; set; }

        [NotNull] public byte[] PeerPublicKey { get; set; }
    }
}
