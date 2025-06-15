using MessagePack;

namespace ArqanumCore.Dtos.Account
{
    [MessagePackObject]
    internal class UpdateUsernameRequestDto
    {
        [Key(0)] public string AccountId { get; set; }

        [Key(1)] public string NewUsername { get; set; }

        [Key(2)] public long Timestamp { get; set; }
    }
}
