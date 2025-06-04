using ArqanumCore.Crypto;
using MessagePack;
using Org.BouncyCastle.Crypto.Parameters;

namespace ArqanumCore.Services
{
    public class ApiService(HttpClient httpClient, MLDsaKeyService mLDsaKey)
    {
        public async Task<HttpResponseMessage> PostAsync<TPayload>(TPayload payload, MLDsaPrivateKeyParameters sPrK, string route)
        {
            byte[] msgpackBytes = MessagePackSerializer.Serialize(payload);
            var httpContent = new ByteArrayContent(msgpackBytes);
            var signature = mLDsaKey.Sign(msgpackBytes, sPrK);
            httpContent.Headers.Add("X-Signature", Convert.ToBase64String(signature));
            return await httpClient.PostAsync("https://enigram-001-site1.qtempurl.com/api/" + route, httpContent);
        }
    }
}
