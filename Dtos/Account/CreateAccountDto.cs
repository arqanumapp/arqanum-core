using MessagePack;

namespace ArqanumCore.Dtos.Account
{
    [MessagePackObject]
    internal class CreateAccountDto
    {
        [Key(0)] public string AccountId { get; set; }

        [Key(1)] public string Username { get; set; }

        [Key(2)] public string? FirstName { get; set; }

        [Key(3)] public string? LastName { get; set; }

        [Key(4)] public byte[] SignaturePublicKey { get; set; }

        [Key(5)] public string ProofOfWork { get; set; }

        [Key(6)] public string ProofOfWorkNonce { get; set; }

        [Key(7)] public string CaptchaToken { get; set; }

        [Key(8)] public long Timestamp { get; set; }
    }
}
