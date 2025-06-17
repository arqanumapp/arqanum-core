using MessagePack;

namespace ArqanumCore.Dtos.Hub
{
    [MessagePackObject]
    internal class HubConnectionAuth
    {
        [Key(0)] public string AccountId { get; set; }

        [Key(1)] public long Timestamp { get; set; }

        [Key(2)] public byte[] RandomBytes { get; set; }
    }
}
