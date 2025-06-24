namespace ArqanumCore.ViewModels.Contact
{
    public class ContactsItemViewModel
    {
        public string ContactId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;

        public string? FullName { get; set; }
    }
}
