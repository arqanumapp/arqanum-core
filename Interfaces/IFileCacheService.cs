namespace ArqanumCore.Interfaces
{
    public interface IFileCacheService
    {
        Task<string?> GetOrDownloadFilePathAsync(string url, string fileName);

        Task<bool> RefreshCacheAsync(string url, string fileName);

        Task<bool> DeleteCachedFileAsync(string fileName);

        Task ClearCacheAsync();

        Task<Stream?> GetFileStreamAsync(string fileName);

        Task<long> GetCacheSizeAsync();

        string GetFileNameFromUrl(string url, string? fallbackName = null);
    }
}
