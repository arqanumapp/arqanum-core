using ArqanumCore.Crypto;
using ArqanumCore.Interfaces;
using ArqanumCore.Services;
using ArqanumCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ArqanumCore
{
    public static class Extensions
    {
        public static IServiceCollection AddArqanumCore(this IServiceCollection services)
        {
            services.AddStorageServices();
            services.AddCryptoServices();
            services.AddLogicServices();

            services.AddHttpClient<ApiService>();

            var provider = services.BuildServiceProvider();

            _ = provider.GetService<IDbPasswordProvider>()
              ?? throw new InvalidOperationException("You must register an implementation of IDbPasswordProvider before calling AddArqanumCore().");



            return services;
        }
        private static IServiceCollection AddLogicServices(this IServiceCollection services)
        {
            services.AddSingleton<SessionKeyStore>();

            services.AddTransient<ProofOfWorkService>();
            services.AddTransient<AccountService>();
            services.AddTransient<ContactService>();
            services.AddTransient<ChatService>();

            return services;
        }
        private static IServiceCollection AddCryptoServices(this IServiceCollection services)
        {
            services.AddSingleton<ShakeHashService>();
            services.AddSingleton<MLDsaKeyService>();
            services.AddSingleton<MLKemKeyService>();
            services.AddSingleton<AesGCMKeyService>();

            return services;
        }

        private static IServiceCollection AddStorageServices(this IServiceCollection services)
        {
            services.AddSingleton<AccountStorage>();
            services.AddSingleton<ContactStorage>();
            services.AddSingleton<ChatStorage>();
            services.AddSingleton<MessageStorage>();

            return services;
        }
    }
}
