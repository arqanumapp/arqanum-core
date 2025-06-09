using MessagePack;

namespace ArqanumCore.Dtos.Contact
{
    [MessagePackObject]
    public class GetContactRequestDto
    {
        [Key(0)] public string ContactIdentifier { get; set; }

        [Key(1)] public string AccountId { get; set; }

        [Key(2)] public long Timestamp { get; set; }
    }
}
