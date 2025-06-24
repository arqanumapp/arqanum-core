using SQLite;

namespace ArqanumCore.InternalModels
{
    [Table("Contacts")]
    public class Contact
    {
        [PrimaryKey, NotNull] public string ContactId { get; set; }

        [NotNull, MaxLength(32)] public string Username { get; set; }

        [NotNull] public string AvatarUrl { get; set; }

        [MaxLength(32)] public string? FirstName { get; set; }

        [MaxLength(32)] public string? LastName { get; set; }

        [MaxLength(150)] public string? Bio { get; set; }

        public byte[]? SignaturePublicKey { get; set; }

        public byte[]? ContactPublicKey { get; set; }

        public byte[]? MyPublicKey { get; set; }

        public byte[]? MyPrivateKey { get; set; }

        [NotNull] public long Version { get; set; }

        [NotNull] public ContactStatus Status { get; set; }
    }

    public enum ContactStatus
    {
        Pending = 0,
        Confirmed = 1,
        Blocked = 2,
        Request = 3,
    }
}
