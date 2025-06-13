namespace ArqanumCore.Services
{
    public static class TimestampValidator
    {
        public static bool IsValid(long timestamp)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Abs(now - timestamp) <= 30;
        }
    }
}
