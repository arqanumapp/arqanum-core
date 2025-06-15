using MessagePack;

namespace ArqanumCore.Dtos.Account
{
    [MessagePackObject]
    public class UpdateBioRequestDto
    {
        [Key(0)] public string AccountId { get; set; }

        [Key(1)] public string Bio { get; set; }

        [Key(2)] public long Timestamp { get; set; }
    }
}
