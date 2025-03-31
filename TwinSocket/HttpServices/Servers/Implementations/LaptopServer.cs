using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RelayProtection.Services.HttpServices.Abstract;
using RelayProtection.Services.HttpServices.Commands;
using RelayProtection.Services.HttpServices.Networking;
using RelayProtection.Services.HttpServices.Notification;
using RelayProtection.Services.HttpServices.Servers.Interfaces;

namespace RelayProtection.Services.HttpServices.Servers.Implementations
{

    public class LaptopServer : ILaptopServer, IAsyncDisposable
    {
        public event Action<bool>? ConnectionStatusChanged;
        public event Action<bool>? ServerStatusChanged;
        private readonly LaptopServerOptions _options;
        private readonly string _httpListenerPrefixForAnyHost;
        private readonly string _httpListenerPrefixForLocalHostOnly;
        private readonly HttpListener _httpListener;
        private readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Shared;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _webSocketLock = new();

        private CancellationTokenSource _udpBroadcastCts = new CancellationTokenSource();
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isServerStarted;
       // private volatile WebSocket? _webSocketClient;
        private volatile bool _disposed;
        public CommandHandlerRegistry CommandHandlerRegistry { get; private set; }

        private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
        //private readonly ConcurrentDictionary<string, CancellationTokenSource> _clientTokens = new();

        private NetworkMonitor? _networkMonitor;
        public LaptopServer(LaptopServerOptions options, LaptopServer logger, CommandHandlerRegistry commandHandlerRegistry)
        {
            _options = options;
            _httpListener = new HttpListener();
            _cancellationTokenSource = new CancellationTokenSource();
            CommandHandlerRegistry = commandHandlerRegistry;
            _httpListenerPrefixForAnyHost = CreateHttpListenerPrefix("+");
            _httpListenerPrefixForLocalHostOnly = CreateHttpListenerPrefix("localhost");
            ConnectionStatusChanged += HandleConnectionStatusChanged;
            InitializeHttpServer();
        }

        /// <summary>
        /// Обрабатывает изменение состояния подключения сервера.
        /// Если подключение установлено, перезапускает `CancellationTokenSource` для UDP.
        /// Если подключение потеряно, перезапускает сервисы.
        /// </summary>
        /// <param name="isConnected">Статус подключения: `true` - подключен, `false` - отключен.</param>
        private async void HandleConnectionStatusChanged(bool isConnected)
        {
            if (isConnected)
            {
                RestartUdpBroadcasting();
                return;
            }
            await Task.Run(() =>
            {
                _webSocketClient?.Abort();
                _webSocketClient?.Dispose();
                _webSocketClient = null;

            });
        }


        private async Task CheckClientConnectionStatusAsync(string clientAddress, int clientPort, CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationTokenSource.Token).ConfigureAwait(false);

                bool isClientAvailable = await IsClientAvailableAsync(clientAddress).ConfigureAwait(false);

                if (_webSocketClient == null || _webSocketClient.State != WebSocketState.Open || !isClientAvailable)
                {
                    Debug.WriteLine($"⚠️ Клиент ({clientAddress}:{clientPort}) отключен. Закрываем WebSocket.");
                    ConnectionStatusChanged?.Invoke(false);
                    return;
                }
            }
        }


        private async Task<bool> IsClientAvailableAsync(string clientAddress)
        {
            try
            {
                using Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(clientAddress, 1000); // Таймаут 1 секунда
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка при проверке доступности клиента {clientAddress}: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Перезапускает `CancellationTokenSource` для UDP-вещания.
        /// </summary>
        private void RestartUdpBroadcasting()
        {
            _udpBroadcastCts?.Cancel();
            _udpBroadcastCts?.Dispose();
            _udpBroadcastCts = new CancellationTokenSource();
        }

 

        /// <summary>
        /// Асинхронно перезапускает сервисы сервера.
        /// </summary>
        private async Task RestartServerServicesAsync()
        {
            await StartServices(_udpBroadcastCts);
        }


        public async Task StartAsync(CancellationTokenSource cancellationToken)
        {
            if (_isServerStarted)
            {
                Debug.WriteLine("⚠️ Сервер уже запущен, повторный запуск не требуется.");
                return;
            }

            _isServerStarted = true;


            while (_isServerStarted)
            {
                try
                {
                    _cancellationTokenSource = cancellationToken;

                    Debug.WriteLine($"🟡 Попытка запуска сервера: http://{Environment.MachineName.ToLower()}:{_options.HttpPort}/");
                    EnsureAdminRightsAndFirewall();

                    await StartServices(_udpBroadcastCts);

                    _networkMonitor = new NetworkMonitor(async () =>
                    {
                        ConnectionStatusChanged?.Invoke(false);
                    });
                    _networkMonitor.StartMonitoring();

                    await HandleRequestsAsync();
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"⚠️ Сервер был остановлен: {ex.Message}");
                    _isServerStarted = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                    ServerStatusChanged?.Invoke(false);
                    await Task.Delay(_options.ReconnectDelayMs, cancellationToken.Token);
                }
            }
        }

        public async Task<bool> SendCommandToClientAsync(IWebCommand command, CancellationTokenSource cancellationTokenSource)
        {
            if (!_isServerStarted)
            {
                Debug.WriteLine("⚠️ Попытка отправки команды, но сервер не запущен.");
                return false;
            }

            if (!CheckWebSocketOpen())
            {
                Debug.WriteLine("⚠️ WebSocket не открыт, отправка отменена.");
                return false;
            }


            try
            {
                string message = command.ToSerializedString();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                ArraySegment<byte> buffer = new(messageBytes);

                await _sendSemaphore.WaitAsync(cancellationTokenSource.Token);

                await _webSocketClient!.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource.Token);

                Debug.WriteLine("Broadcasted command: {Message}", message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при отпраке : {ex.Message}");
                return false;
            }
            finally
            {
                _sendSemaphore.Release();
            }

            return true;
        }
        public void Stop()
        {
            if (!_isServerStarted)
            {
                Debug.WriteLine("⚠️ Сервер уже остановлен.");
                return;
            }

            _isServerStarted = false;
            ResetCancellationTokenSource();
            HandleServerStarted(false);
            Debug.WriteLine("🛑 WebSocket сервер остановлен.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _networkMonitor?.Dispose();

            if (CheckWebSocketOpen())
            {
                await _webSocketClient!.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Disposed",
                    CancellationToken.None
                );
                _webSocketClient.Dispose();
            }

            ResetCancellationTokenSource();
            _webSocketClient = null;

            if (_httpListener.IsListening)
                _httpListener.Stop();

            _udpBroadcastCts?.Cancel();
            _udpBroadcastCts?.Dispose();

            _httpListener.Close();
            await _udpDiscovery.DisposeAsync();
            _disposed = true;

            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }

        private bool CheckWebSocketOpen()
        {
            lock (_webSocketLock)
            {
                if (_webSocketClient == null || _webSocketClient.State == WebSocketState.Aborted)
                {
                    Debug.WriteLine("⚠️ WebSocket в состоянии Aborted. Сбрасываем соединение.");
                    ResetWebSocketAsync().Wait();
                    return false;
                }
                return _webSocketClient.State == WebSocketState.Open;
            }
        }

        private void ResetCancellationTokenSource()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private string CreateHttpListenerPrefix(string host)
        {
            return $"http://{host}:{_options.HttpPort}/";
        }

        private void HandleServerStarted(bool isServerStarted)
        {
            _isServerStarted = isServerStarted;
        }

        private void InitializeHttpServer()
        {
            _httpListener.Prefixes.Add(_httpListenerPrefixForAnyHost);
            _httpListener.Prefixes.Add(_httpListenerPrefixForLocalHostOnly);
        }

        private void EnsureAdminRightsAndFirewall()
        {
            string[] urls = [_httpListenerPrefixForAnyHost, _httpListenerPrefixForLocalHostOnly];
            AdminRightsManager.EnsureAdminRights(urls, _options.FirewallRuleName, _options.HttpPort);
            AdminRightsManager.EnsureAdminRights([], $"{_options.FirewallRuleName}_UDP", _options.ServerDiscoveryPort);
            PowerShellLauncher.DisableActiveProbing();
        }

        /// <summary>
        /// Запускает основные сервисы, включая UDP Discovery и HTTP-сервер.
        /// </summary>
        /// <param name="linkedCts">Токен отмены, связанный с процессом.</param>
        private async Task StartServices(CancellationTokenSource linkedCts)
        {
            _httpListener.Start();
            ServerStatusChanged?.Invoke(true);
            HandleServerStarted(true);
            Debug.WriteLine($"✅ Сервер запушен: {CreateHttpListenerPrefix(Environment.MachineName.ToLower())}");
           Task.Run(async () => await _udpDiscovery.StartBroadcasting(linkedCts.Token));
        }

        private async Task HandleRequestsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        await HandleWebSocketRequestAsync(context);
                    }
                    else
                    {
                        HandleHttpRequest(context);
                    }
                }
                catch (Exception ex)
                {
                    ConnectionStatusChanged?.Invoke(false);
                    Debug.WriteLine($"❌ Ошибка при обработке запроса: {ex.Message}");
                }
            }
        }

        private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            _webSocketClient = webSocketContext.WebSocket;
            ConnectionStatusChanged?.Invoke(true);

            string clientAddress = context.Request.RemoteEndPoint?.Address.ToString() ?? "Unknown";
            int clientPort = context.Request.RemoteEndPoint?.Port ?? 0;

            Debug.WriteLine($"✅ Клиент подключился: {clientAddress}:{clientPort} соединение: {_webSocketClient.State}");

            _ = Task.Run(() => CheckClientConnectionStatusAsync(clientAddress, clientPort, _cancellationTokenSource));
            _ = Task.Run(() => HandleClientAsync(_webSocketClient, _cancellationTokenSource.Token));
        }

        private static void HandleHttpRequest(HttpListenerContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            context.Response.Close();
        }

        private async Task HandleClientAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            byte[] buffer = _byteArrayPool.Rent(_options.BufferSize);

            try
            {
                while (!cancellationToken.IsCancellationRequested && CheckWebSocketOpen())
                {
                    WebSocketReceiveResult? result = await ReceiveDataAsync(webSocket, buffer);

                    // Если клиент разорвал соединение, WebSocket закрыт
                    if (result == null || webSocket.State == WebSocketState.Aborted)
                    {
                        Debug.WriteLine("⚠️ WebSocket соединение закрыто или в состоянии Aborted.");
                        ConnectionStatusChanged?.Invoke(false);
                        await ResetWebSocketAsync();
                        break;
                    }

                    Debug.WriteLine($"📩 Получено сообщение ({result.Count} байт) от клиента.");

                    await ProcessReceivedMessageAsync(buffer, result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка WebSocket клиента: {ex.Message}");
                ConnectionStatusChanged?.Invoke(false);
                await ResetWebSocketAsync();
            }
            finally
            {
                _byteArrayPool.Return(buffer);
            }
        }

        private async Task ResetWebSocketAsync()
        {
            if (_webSocketClient != null)
            {
                try
                {
                    if (_webSocketClient.State != WebSocketState.Closed)
                    {
                        await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сбрасываем соединение", CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Ошибка при закрытии WebSocket: {ex.Message}");
                }
                finally
                {
                    _webSocketClient.Dispose();
                    _webSocketClient = null;
                }
            }

            Debug.WriteLine("🔄 Ждем нового подключения WebSocket...");
        }



        private async Task<WebSocketReceiveResult?> ReceiveDataAsync(WebSocket webSocket, byte[] buffer)
        {
            try
            {
                return await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Debug.WriteLine("⚠️ Превышено время ожидания получения данных через WebSocket.");
                return null;
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(false);
                Debug.WriteLine($"❌ Ошибка при получении данных от клиента: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessReceivedMessageAsync(byte[] buffer, WebSocketReceiveResult result)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                Type commandType = CommandHandlerRegistry.ExtractCommandType(message);
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true
                };

                if (JsonSerializer.Deserialize(message, commandType, jsonSerializerOptions) is IWebCommand command)
                {
                    Debug.WriteLine("✅ Команда успешно десериализована и передана на выполнение.");
                    await CommandHandlerRegistry.ExecuteCommandAsync(command);
                }
                else
                {
                    Debug.WriteLine("⚠️ Не удалось десериализовать команду.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка при обработке команды: {ex.Message}");
            }
        }
    }
}
