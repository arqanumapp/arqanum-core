namespace ArqanumCore.Dtos.Contact
{
    public class GetContactResponceDto
    {
        public string AvatarUrl { get; set; }

        public string ContactId { get; set; }

        public string Username { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Bio { get; set; }

        public long Version { get; set; }

        public long Timestamp { get; set; }
    }
}
