namespace ArqanumCore.Interfaces
{
    public interface IDbPasswordProvider
    {
        Task<string> GetDatabasePassword();
    }
}
