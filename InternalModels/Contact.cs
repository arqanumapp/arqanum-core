using SQLite;

namespace ArqanumCore.InternalModels
{
    [Table("Contacts")]
    public class Contact
    {
        [PrimaryKey, NotNull] public string ContactId { get; set; }

        [NotNull, MaxLength(32)] public string Username { get; set; }

        [MaxLength(32)] public string? FirstName { get; set; }

        [MaxLength(32)] public string? LastName { get; set; }

        [MaxLength(150)] public string? Bio { get; set; }

        [NotNull] public byte[] SignaturePublicKey { get; set; }
    }
}
