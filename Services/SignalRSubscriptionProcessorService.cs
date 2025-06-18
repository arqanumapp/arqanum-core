using ArqanumCore.Dtos.Contact;
using ArqanumCore.Dtos.Hub.Contact;
using MessagePack;
using System.Diagnostics;

namespace ArqanumCore.Services
{
    public interface ISignalRSubscriptionProcessorService
    {
        Task Contact(byte[] data);
    }

    internal class SignalRSubscriptionProcessorService(ContactService contactService) : ISignalRSubscriptionProcessorService
    {
        public async Task Contact(byte[] data)
        {
            try
            {
                var contactBaseMessage = MessagePackSerializer.Deserialize<BaseContactHubMessage>(data);
                if (!TimestampValidator.IsValid(contactBaseMessage.Timestamp))
                {
                    return;
                }
                switch (contactBaseMessage.MessageType)
                {
                    case ContactHubMessageType.NewContactRequest:
                        var contactMessage = MessagePackSerializer.Deserialize<ContactPayload>(contactBaseMessage.Payload);

                        break;

                    case ContactHubMessageType.ConfirmedContactRequest:
                        var contactUpdateMessage = MessagePackSerializer.Deserialize<ContactPayload>(contactBaseMessage.Payload);

                        break;

                    default:
                        Debug.WriteLine($"Unknown contact message type: {contactBaseMessage.MessageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRSubscriptionProcessorService:Contact] Error: {ex}");
            }
        }
    }
}
