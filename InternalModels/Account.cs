using SQLite;

namespace ArqanumCore.InternalModels
{
    [Table("Account")]
    public class Account
    {
        [PrimaryKey, NotNull] public string AccountId { get; set; }

        [NotNull, MaxLength(32)] public string Username { get; set; }

        [NotNull] public string AvatarUrl { get; set; }

        [MaxLength(32)] public string? FirstName { get; set; }

        [MaxLength(32)] public string? LastName { get; set; }

        [MaxLength(150)] public string? Bio { get; set; }

        [NotNull] public byte[] SignaturePublicKey { get; set; }

        [NotNull] public byte[] SignaturePrivateKey { get; set; }

        public long Version { get; set; }
    }
}
