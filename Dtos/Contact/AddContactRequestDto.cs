using MessagePack;

namespace ArqanumCore.Dtos.Contact
{
    [MessagePackObject]
    internal class AddContactRequestDto
    {
        [Key(0)] public string RecipientId { get; set; }

        [Key(1)] public byte[] Payload { get; set; }

        [Key(2)] public byte[] PayloadSignature { get; set; }

        [Key(3)] public long Timestamp { get; set; }
    }

    [MessagePackObject]
    internal class ContactPayload
    {
        [Key(0)] public string SenderId { get; set; }

        [Key(1)] public byte[] SignaturePublicKey { get; set; }

        [Key(2)] public byte[] ContactPublicKey { get; set; }
    }
}
