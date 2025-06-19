using ArqanumCore.Crypto;
using ArqanumCore.Dtos.Hub;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ArqanumCore.Services
{
    public interface ISignalRClientService
    {
        Task StartAsync();
        Task StopAsync();
        Task SendAsync(string method, params object[] args);
        void On<T>(string method, Action<T> handler);
        bool IsConnected { get; }
        event Func<Exception?, Task>? Reconnecting;
        event Func<string?, Task>? Reconnected;
    }

    public class SignalRClientService(MLDsaKeyService mLDsaKeyService, SessionKeyStore sessionKeyStore, ISignalRSubscriptionProcessorService subscriptionProcessor) : ISignalRClientService, IDisposable
    {
        public event Func<Exception?, Task>? Reconnecting;
        public event Func<string?, Task>? Reconnected;

        private HubConnection? _connection;

        private void ConfigureSubscriptions()
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection must be initialized");

            _connection.On<byte[]>("Contact", async (data) =>
            {
                await subscriptionProcessor.Contact(data);
            });
        }

        private string GenerateToken()
        {
            try
            {
                var auth = new HubConnectionAuth
                {
                    AccountId = sessionKeyStore.GetId(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    RandomBytes = GenerateRandomBytes()
                };

                var authBytes = MessagePackSerializer.Serialize(auth);
                var authBase64 = Convert.ToBase64String(authBytes);
                var signatureBytes = mLDsaKeyService.Sign(authBytes, sessionKeyStore.GetPrivateKey());
                var signatureBase64 = Convert.ToBase64String(signatureBytes);

                return $"{authBase64}|{signatureBase64}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:GenerateToken] Error: {ex}");
                throw;
            }
        }

        private HubConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    try
                    {
                        _connection = new HubConnectionBuilder()
                            .WithUrl("https://arqanumapp-001-site1.qtempurl.com/hub/app", options =>
                            {
                                options.AccessTokenProvider = async () =>
                                {
                                    try
                                    {
                                        return await Task.FromResult(GenerateToken());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[AccessTokenProvider] Error: {ex}");
                                        throw;
                                    }
                                };
                            })
                            .WithAutomaticReconnect()
                            .Build();

                        ConfigureSubscriptions();

                        _connection.Reconnecting += async (error) =>
                        {
                            Debug.WriteLine($"[SignalR Reconnecting] Reason: {error}");
                            if (Reconnecting != null)
                                await Reconnecting.Invoke(error);
                        };

                        _connection.Reconnected += async (connectionId) =>
                        {
                            Debug.WriteLine($"[SignalR Reconnected] NewConnectionId: {connectionId}");
                            if (Reconnected != null)
                                await Reconnected.Invoke(connectionId);
                        };

                        _connection.Closed += async (error) =>
                        {
                            Debug.WriteLine($"[SignalR Closed] Reason: {error}");
                            await Task.CompletedTask;
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SignalRClientService:Connection Init] Error: {ex}");
                        throw;
                    }
                }

                return _connection;
            }
        }

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task StartAsync()
        {
            try
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                if (_connection == null || _connection.State == HubConnectionState.Disconnected)
                {
                    await Connection.StartAsync();
                    Debug.WriteLine("[SignalR Start] Connected.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:StartAsync] Error: {ex}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_connection != null)
                {
                    await _connection.StopAsync();
                    Debug.WriteLine("[SignalR Stop] Stopped.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:StopAsync] Error: {ex}");
                throw;
            }
        }

        public Task SendAsync(string method, params object[] args)
        {
            try
            {
                if (_connection == null)
                    throw new InvalidOperationException("SignalR connection is not created yet. Call StartAsync first.");

                return _connection.SendAsync(method, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:SendAsync] Method: {method}, Error: {ex}");
                throw;
            }
        }

        public void On<T>(string method, Action<T> handler)
        {
            try
            {
                if (_connection == null)
                    throw new InvalidOperationException("SignalR connection is not created yet. Call StartAsync first.");

                _connection.On(method, handler);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:On] Method: {method}, Error: {ex}");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    _connection = null;
                    Debug.WriteLine("[SignalR Dispose] Disposed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalRClientService:Dispose] Error: {ex}");
                throw;
            }
        }

        public static byte[] GenerateRandomBytes()
        {
            try
            {
                int length = RandomNumberGenerator.GetInt32(20, 101);
                var bytes = new byte[length];
                RandomNumberGenerator.Fill(bytes);
                return bytes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GenerateRandomBytes] Error: {ex}");
                throw;
            }
        }
    }
}
