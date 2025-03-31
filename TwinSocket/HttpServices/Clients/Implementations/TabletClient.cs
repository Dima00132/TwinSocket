using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RelayProtection.Services.HttpServices.Abstract;
using RelayProtection.Services.HttpServices.Clients.Interfaces;
using RelayProtection.Services.HttpServices.Commands;
using RelayProtection.Services.HttpServices.Notification;
using RelayProtection.Services.HttpServices.Servers.Implementations;

namespace RelayProtection.Services.HttpServices.Clients.Implementations
{
    public class TabletClient<T> : ITabletClient
    {
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _connectionTokenSource;

        private readonly LaptopServerOptions _laptopServerOptions;

        private bool _disposed;
        public CommandHandlerRegistry CommandHandlerRegistry { get; }
        public event Action<bool> ConnectionStatusChanged;

        public TabletClient(LaptopServerOptions laptopServerOptions,CommandHandlerRegistry commandHandlerRegistry)
        {
            _laptopServerOptions = laptopServerOptions;
            CommandHandlerRegistry = commandHandlerRegistry;
        }

        private async Task<bool> IsServerAvailableAsync(string serverAddress, int port)
        {
            try
            {
                using TcpClient tcpClient = new TcpClient();
                Task connectTask = tcpClient.ConnectAsync(serverAddress, port);
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
                Task completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                return completedTask == connectTask && tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private async Task MonitorConnectionAsync(string serverAddress, int serverPort, CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token).ConfigureAwait(false);
                bool isTcpAvailable = await IsServerAvailableAsync(serverAddress, serverPort).ConfigureAwait(false);
                if (_webSocket?.State != WebSocketState.Open || !isTcpAvailable)
                {
                    Debug.WriteLine($"⚠️ Сервер недоступен. Закрываем WebSocket.");
                    await DisconnectServerAsync().ConfigureAwait(false);
                    return;
                }
            }
        }

        public async Task ConnectToServerAsync()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                Debug.WriteLine("✅ Уже подключено к серверу");
                return;
            }

            _webSocket = new ClientWebSocket();
            _connectionTokenSource = new CancellationTokenSource();
            CancellationTokenSource timeoutCts = new CancellationTokenSource(1000);

            try
            {
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _connectionTokenSource.Token, timeoutCts.Token);

                await _webSocket.ConnectAsync(
                    new Uri($"ws://{_laptopServerOptions.ServerAddress}:{_laptopServerOptions.HttpPort}/"),
                    linkedCts.Token
                );

                ConnectionStatusChanged?.Invoke(true);
                StartListeningForMessages(_webSocket, _connectionTokenSource);
                _ = MonitorConnectionAsync(
                    _laptopServerOptions.ServerAddress,
                    _laptopServerOptions.HttpPort,
                    _connectionTokenSource
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка подключения к серверу: {ex.Message}");
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        private void StartListeningForMessages(
            ClientWebSocket clientWebSocket,
            CancellationTokenSource cancellationTokenSource
        )
        {
            _ = Task.Run(
                async () =>
                {
                    byte[] buffer = _arrayPool.Rent(4096);

                    try
                    {
                        while (
                            clientWebSocket?.State == WebSocketState.Open
                            && !cancellationTokenSource.Token.IsCancellationRequested
                        )
                        {
                            WebSocketReceiveResult receiveResult = await clientWebSocket.ReceiveAsync(
                                new ArraySegment<byte>(buffer),
                                cancellationTokenSource.Token
                            );
                            if (receiveResult.MessageType == WebSocketMessageType.Close)
                                continue;
                            string message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                            Debug.WriteLine($"📩 Получено сообщение: {message}");
                            _ = Task.Run(() => ProcessReceivedMessage(message));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Ошибка приема сообщения: {ex.Message}");
                    }
                    finally
                    {
                        _arrayPool.Return(buffer);
                        await DisconnectServerAsync();
                    }
                },
                cancellationTokenSource.Token
            );
        }

        public async Task DisconnectServerAsync()
        {
            if (_webSocket != null)
            {
                Debug.WriteLine($"⚠️ Отключение от сервера");
                try
                {
                    _connectionTokenSource?.Cancel();

                    if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Закрытие соединения",
                            CancellationToken.None
                        );
                        Debug.WriteLine($"✅ Соединение с сервером закрыто корректно.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка при закрытии соединения: {ex.Message}");
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                    _connectionTokenSource?.Dispose();
                    _connectionTokenSource = null;
                }

                ConnectionStatusChanged?.Invoke(false);
            }
        }

        public async Task<bool> SendCommandToServerAsync(IWebCommand command)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.WriteLine("⚠️ Сервер недоступен или WebSocket закрыт");
                return false;
            }

            try
            {
                string message = command.ToSerializedString();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                ArraySegment<byte> buffer = new ArraySegment<byte>(messageBytes);

                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _connectionTokenSource!.Token);
                Debug.WriteLine($"✅ Команда отправлена серверу: {message}");
                return true;
            }
            catch (WebSocketException wex)
            {
                Debug.WriteLine($"❌ WebSocketException при отправке команды: {wex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка при отправке команды: {ex.Message}");
            }

            return false;
        }

        private void ProcessReceivedMessage(string message)
        {
            try
            {
                Type commandType = CommandHandlerRegistry.ExtractCommandType(message);
                JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
                IWebCommand? command = JsonSerializer.Deserialize(message, commandType, jsonSerializerOptions) as IWebCommand;
                if (command != null)
                {
                    Debug.WriteLine("✅ Команда успешно обработана.");
                    CommandHandlerRegistry.ExecuteCommand(_laptopServerOptions.ServerAddress, command);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка обработки сообщения: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await DisconnectServerAsync();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
