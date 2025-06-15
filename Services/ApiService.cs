using ArqanumCore.Crypto;
using MessagePack;
using Org.BouncyCastle.Crypto.Parameters;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ArqanumCore.Services
{
    public class ApiService(HttpClient httpClient, MLDsaKeyService mLDsaKey)
    {
        private const string BaseUrl = "https://arqanumapp-001-site1.qtempurl.com/api/";

        public async Task<HttpResponseMessage?> PostSignBytesAsync<TPayload>(TPayload payload, MLDsaPrivateKeyParameters sPrK, string route)
        {
            try
            {
                byte[] msgpackBytes = MessagePackSerializer.Serialize(payload);
                var httpContent = new ByteArrayContent(msgpackBytes);
                var signature = mLDsaKey.Sign(msgpackBytes, sPrK);
                httpContent.Headers.Add("X-Signature", Convert.ToBase64String(signature));
                return await httpClient.PostAsync(BaseUrl + route, httpContent);
            }
            catch
            {
                return null;
            }
        }

        public async Task<HttpResponseMessage?> PostJsonAsync<TPayload>(TPayload payload, string route)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                var httpContent = new ByteArrayContent(jsonBytes);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                return await httpClient.PostAsync(BaseUrl + route, httpContent);
            }
            catch
            {
                return null;
            }
        }
    }
}
