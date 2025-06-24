using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Contact;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Contact;
using MessagePack;
using System.Collections.ObjectModel;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace ArqanumCore.Services
{
    public class ContactService(
        ContactStorage contactStorage,
        ApiService apiService,
        SessionKeyStore sessionKeyStore,
        MLKemKeyService mLKemKeyService,
        MLDsaKeyService mLDsaKeyService,
        ShakeHashService shakeHashService,
        IShowNotyficationService showNotyficationService)
    {
        public ObservableCollection<ContactsItemViewModel> ConfirmedContacts { get; } = [];
        public ObservableCollection<ContactsItemViewModel> PendingContacts { get; } = [];
        public ObservableCollection<ContactsItemViewModel> RequestContacts { get; } = [];
        public ObservableCollection<ContactsItemViewModel> BlockedContacts { get; } = [];

        private int _requestContactsCount;

        public int RequestContactsCount
        {
            get => _requestContactsCount;
            set
            {
                if (_requestContactsCount != value)
                {
                    _requestContactsCount = value;
                    RequestContactsCountChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int>? RequestContactsCountChanged;


        public async Task<bool> LoadContactsAsync()
        {
            var contacts = await contactStorage.GetAllContactsAsync();

            var confirmed = new List<ContactsItemViewModel>();
            var pending = new List<ContactsItemViewModel>();
            var requests = new List<ContactsItemViewModel>();
            var blocked = new List<ContactsItemViewModel>();

            foreach (var contact in contacts)
            {
                var item = new ContactsItemViewModel
                {
                    ContactId = contact.ContactId,
                    Username = contact.Username,
                    AvatarUrl = contact.AvatarUrl,
                    FullName = $"{contact.FirstName} {contact.LastName}".Trim()
                };

                switch (contact.Status)
                {
                    case ContactStatus.Confirmed: confirmed.Add(item); break;
                    case ContactStatus.Pending: pending.Add(item); break;
                    case ContactStatus.Request: requests.Add(item); break;
                    case ContactStatus.Blocked: blocked.Add(item); break;
                }
            }

            ConfirmedContacts.Clear(); foreach (var c in confirmed) ConfirmedContacts.Add(c);
            PendingContacts.Clear(); foreach (var c in pending) PendingContacts.Add(c);
            RequestContacts.Clear(); foreach (var c in requests) RequestContacts.Add(c);
            BlockedContacts.Clear(); foreach (var c in blocked) BlockedContacts.Add(c);

            RequestContactsCount = requests.Count;
            return true;
        }

        public async Task<GetContactResponceDto?> FindContactAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || sessionKeyStore.GetId() == identifier || sessionKeyStore.GetUsername() == identifier)
            {
                return null;
            }

            var payload = new GetContactRequestDto { ContactIdentifier = identifier, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), AccountId = sessionKeyStore.GetId() };

            using var response = await apiService.PostSignBytesAsync(payload, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/find");

            if (response is null || !response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            var contact = JsonSerializer.Deserialize<GetContactResponceDto>(json, options);

            if (!TimestampValidator.IsValid(contact.Timestamp))
                throw new InvalidOperationException("Invalid timestamp in contact response");

            return contact;
        }

        public async Task<bool> AddContactAsync(GetContactResponceDto getContactResponceDto)
        {
            try
            {
                var contact = new Contact();
                contact.ContactId = getContactResponceDto.ContactId;
                contact.Username = getContactResponceDto.Username;
                contact.AvatarUrl = getContactResponceDto.AvatarUrl;
                contact.FirstName = getContactResponceDto.FirstName;
                contact.LastName = getContactResponceDto.LastName;
                contact.Bio = getContactResponceDto.Bio;
                contact.Version = getContactResponceDto.Version;
                contact.Status = ContactStatus.Pending;

                var (PublicKey, PrivateKey) = mLKemKeyService.GenerateKey();

                contact.MyPublicKey = PublicKey.GetEncoded();
                contact.MyPrivateKey = PrivateKey.GetEncoded();

                var payload = new ContactPayload
                {
                    SenderId = sessionKeyStore.GetId(),
                    ContactPublicKey = contact.MyPublicKey,
                    SignaturePublicKey = sessionKeyStore.GetPublicKey().GetEncoded(),
                };
                var payloadBytes = MessagePackSerializer.Serialize(payload);

                var payloadBytesSignature = mLDsaKeyService.Sign(payloadBytes, sessionKeyStore.GetPrivateKey());

                var request = new AddContactRequestDto
                {
                    RecipientId = contact.ContactId,
                    Payload = payloadBytes,
                    PayloadSignature = payloadBytesSignature,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };

                var response = await apiService.PostSignBytesAsync(request, sPrK: sessionKeyStore.GetPrivateKey(), route: "contact/add");

                if (response != null && response.IsSuccessStatusCode)
                {
                    await contactStorage.SaveContactAsync(contact);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task NewContactRequest(byte[] payload, byte[] payloadSignature)
        {
            try
            {
                var newContactMessage = MessagePackSerializer.Deserialize<ContactPayload>(payload);

                if (Convert.ToBase64String(shakeHashService.ComputeHash256(newContactMessage.SignaturePublicKey, 64)) != newContactMessage.SenderId)
                {
                    return;
                }

                if (!mLDsaKeyService.Verify(newContactMessage.SignaturePublicKey, payload, payloadSignature))
                {
                    return;
                }

                var contactInfo = await FindContactAsync(newContactMessage.SenderId);

                if (contactInfo == null)
                {
                    return;
                }

                var contact = new Contact
                {
                    ContactId = contactInfo.ContactId,
                    Username = contactInfo.Username,
                    AvatarUrl = contactInfo.AvatarUrl,
                    FirstName = contactInfo.FirstName,
                    LastName = contactInfo.LastName,
                    Bio = contactInfo.Bio,
                    Version = contactInfo.Version,
                    SignaturePublicKey = newContactMessage.SignaturePublicKey,
                    ContactPublicKey = newContactMessage.ContactPublicKey,
                    Status = ContactStatus.Request,
                };
                await contactStorage.SaveContactAsync(contact);
                showNotyficationService.ShowNotificationAsync("New contact request", "Username");
            }
            catch
            {
                return;
            }
        }

    }
}

