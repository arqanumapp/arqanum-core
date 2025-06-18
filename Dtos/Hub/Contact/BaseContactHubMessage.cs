using MessagePack;

namespace ArqanumCore.Dtos.Hub.Contact
{
    [MessagePackObject]
    internal class BaseContactHubMessage
    {
        [Key(0)] public ContactHubMessageType MessageType { get; set; }

        [Key(1)] public byte[] Payload { get; set; }

        [Key(2)] public byte[] PayloadSignature { get; set; }

        [Key(3)] public long Timestamp { get; set; }
    }
}
