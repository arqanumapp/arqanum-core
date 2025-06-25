using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Contact;
using ArqanumCore.Interfaces;
using ArqanumCore.InternalModels;
using ArqanumCore.Storage;
using ArqanumCore.ViewModels.Contact;
using MessagePack;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class ContactService(
        ContactStorage contactStorage,
        ApiService apiService,
        SessionKeyStore sessionKeyStore,
        MLKemKeyService mLKemKeyService,
        MLDsaKeyService mLDsaKeyService,
        ShakeHashService shakeHashService,
        IShowNotyficationService showNotyficationService,
        IFileCacheService fileCacheService)
    {
        private List<ContactsItemViewModel> _allConfirmed = [];
        private List<ContactsItemViewModel> _allPending = [];
        private List<ContactsItemViewModel> _allRequests = [];
        private List<ContactsItemViewModel> _allBlocked = [];

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
                var cacheFileName = fileCacheService.GetFileNameFromUrl(contact.AvatarUrl, contact.ContactId);
                var localAvatarPath = await fileCacheService.GetOrDownloadFilePathAsync(contact.AvatarUrl, cacheFileName);
                var avatarUrlToUse = localAvatarPath ?? $"{contact.AvatarUrl}?v={contact.Version}";

                var item = new ContactsItemViewModel
                {
                    ContactId = contact.ContactId,
                    Username = contact.Username,
                    AvatarUrl = $"file:///{avatarUrlToUse.Replace("\\", "/")}",
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

            _allConfirmed = confirmed;
            _allPending = pending;
            _allRequests = requests;
            _allBlocked = blocked;

            ApplyConfirmedFilter("");
            ApplyPendingFilter("");
            ApplyRequestFilter("");
            ApplyBlockedFilter("");

            RequestContactsCount = requests.Count;
            return true;
        }

        #region Filters

        public void ApplyPendingFilter(string query)
        {
            PendingContacts.Clear();
            var filtered = _allPending
                .Where(c =>
                    string.IsNullOrWhiteSpace(query) ||
                    c.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.FullName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var contact in filtered)
                PendingContacts.Add(contact);
        }

        public void ApplyConfirmedFilter(string query)
        {
            ConfirmedContacts.Clear();
            var filtered = _allConfirmed
                .Where(c =>
                    string.IsNullOrWhiteSpace(query) ||
                    c.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.FullName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var contact in filtered)
                ConfirmedContacts.Add(contact);
        }

        public void ApplyRequestFilter(string query)
        {
            RequestContacts.Clear();
            var filtered = _allRequests
                .Where(c =>
                    string.IsNullOrWhiteSpace(query) ||
                    c.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.FullName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var contact in filtered)
                RequestContacts.Add(contact);
        }


        public void ApplyBlockedFilter(string query)
        {
            BlockedContacts.Clear();
            var filtered = _allBlocked
                .Where(c =>
                    string.IsNullOrWhiteSpace(query) ||
                    c.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.FullName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var contact in filtered)
                BlockedContacts.Add(contact);
        }

        #endregion

        public async Task<GetContactResponceDto?> FindContactAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || sessionKeyStore.GetId() == identifier || sessionKeyStore.GetUsername() == identifier)
            {
                return null;
            }
            var localContact = await contactStorage.GetContactAsync(identifier);

            if (localContact != null)
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
                var existingContact = await contactStorage.GetContactAsync(getContactResponceDto.ContactId);

                if (existingContact != null)
                {
                    return false;
                }

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
                    var fileName = fileCacheService.GetFileNameFromUrl(contact.AvatarUrl, contact.ContactId);
                    var cachedPath = await fileCacheService.GetOrDownloadFilePathAsync(contact.AvatarUrl, fileName);

                    if (!string.IsNullOrEmpty(cachedPath))
                    {
                        contact.AvatarUrl = cachedPath;
                    }

                    await contactStorage.SaveContactAsync(contact);

                    var newPendingContact = new ContactsItemViewModel
                    {
                        ContactId = contact.ContactId,
                        Username = contact.Username,
                        AvatarUrl = $"file:///{contact.AvatarUrl.Replace("\\", "/")}",
                        FullName = $"{contact.FirstName} {contact.LastName}".Trim(),
                    };

                    _allPending.Add(newPendingContact);
                    PendingContacts.Add(newPendingContact);

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
                var localContact = await contactStorage.GetContactAsync(newContactMessage.SenderId);

                if (localContact != null)
                {
                    return;
                }

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

                showNotyficationService.ShowNotificationAsync("New contact request", contactInfo.Username);

                var newRequest = new ContactsItemViewModel
                {
                    ContactId = contact.ContactId,
                    Username = contact.Username,
                    AvatarUrl = $"file:///{contact.AvatarUrl.Replace("\\", "/")}",
                    FullName = $"{contact.FirstName} {contact.LastName}".Trim(),
                };

                _allRequests.Add(newRequest);
                RequestContacts.Add(newRequest);
                RequestContactsCount = _allRequests.Count;
            }
            catch
            {
                return;
            }
        }

        #region Requests Contacts Methods

        public async Task<bool> ConfirmContactAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);

                if (localContact == null || localContact.Status != ContactStatus.Request)
                {
                    return false;
                }

                localContact.Status = ContactStatus.Confirmed;
                //TODO: Send confirmation message to the contact
                //await contactStorage.SaveContactAsync(localContact);

                _allConfirmed.Add(contact);
                _allRequests.Remove(contact);

                ConfirmedContacts.Add(contact);
                RequestContacts.Remove(contact);

                RequestContactsCount = _allRequests.Count;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RejectContactAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);
                if (localContact == null || localContact.Status != ContactStatus.Request)
                {
                    return false;
                }
                //await contactStorage.SaveContactAsync(localContact);
                _allRequests.Remove(contact);
                RequestContacts.Remove(contact);
                RequestContactsCount = _allRequests.Count;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RejectAndBlockContactRequestAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);
                if (localContact == null || localContact.Status != ContactStatus.Request)
                {
                    return false;
                }
                localContact.Status = ContactStatus.Blocked;
                //await contactStorage.SaveContactAsync(localContact);
                _allBlocked.Add(contact);
                _allRequests.Remove(contact);
                BlockedContacts.Add(contact);
                RequestContacts.Remove(contact);
                RequestContactsCount = _allRequests.Count;

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Confirmed Contacts Methods

        public async Task<bool> DeleteContactAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);

                if (localContact == null || localContact.Status != ContactStatus.Confirmed)
                {
                    return false;
                }

                //await contactStorage.DeleteContactAsync(localContact.ContactId);

                _allConfirmed.Remove(contact);
                ConfirmedContacts.Remove(contact);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BlockContactAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);
                if (localContact == null || localContact.Status != ContactStatus.Confirmed)
                {
                    return false;
                }
                localContact.Status = ContactStatus.Blocked;
                //await contactStorage.SaveContactAsync(localContact);
                _allBlocked.Add(contact);
                _allConfirmed.Remove(contact);
                BlockedContacts.Add(contact);
                ConfirmedContacts.Remove(contact);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Blocked Contacts Methods

        public async Task<bool> UnblockContactAsync(ContactsItemViewModel contact)
        {
            try
            {
                var localContact = await contactStorage.GetContactAsync(contact.ContactId);
                if (localContact == null || localContact.Status != ContactStatus.Blocked)
                {
                    return false;
                }
                if(localContact.ContactPublicKey != null && localContact.SignaturePublicKey != null && localContact.MyPublicKey != null && localContact.MyPrivateKey != null)
                {
                    localContact.Status = ContactStatus.Confirmed;
                    _allConfirmed.Add(contact);
                    ConfirmedContacts.Add(contact);
                    //await contactStorage.SaveContactAsync(localContact);
                }
                else
                {
                    //await contactStorage.DeleteContactAsync(localContact.ContactId);
                }
                _allBlocked.Remove(contact);
                BlockedContacts.Remove(contact);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

