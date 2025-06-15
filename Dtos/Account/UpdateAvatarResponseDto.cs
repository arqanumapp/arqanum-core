namespace ArqanumCore.Dtos.Account
{
    internal class UpdateAvatarResponseDto
    {
        public string AvatarUrl { get; set; }

        public long Version { get; set; }

        public long Timestamp { get; set; }
    }
}
