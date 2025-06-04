namespace ArqanumCore.ViewModels.Contact
{
    public class GetContactViewModel
    {
        public string ContactId { get; set; }

        public string Username { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Bio { get; set; }

        public bool IsConfirmed { get; set; } 
    }
}
