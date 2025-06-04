namespace ArqanumCore.Interfaces
{
    public interface ICaptchaProvider
    {
        Task<string> GetCaptchaTokenAsync();
    }
}
