using MessagePack;

namespace ArqanumCore.Dtos.Account
{
    [MessagePackObject]
    public class UpdateFullNameRequestDto
    {
        [Key(0)] public string AccountId { get; set; }

        [Key(1)] public string FirstName { get; set; }

        [Key(2)] public string LastName { get; set; }

        [Key(3)] public long Timestamp { get; set; }
    }
}
