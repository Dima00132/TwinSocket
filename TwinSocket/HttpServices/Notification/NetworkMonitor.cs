using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.Servers.Implementations
{
    /// <summary>
    /// Класс NetworkMonitor выполняет мониторинг состояния сети и отслеживает изменения в подключении Wi-Fi.
    /// </summary>
    public class NetworkMonitor : IDisposable
    {
        private string? _currentSsid;
        private readonly Func<Task> _onDisconnectCallback;
        private volatile bool _isConnected = true;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NetworkMonitor"/>.
        /// </summary>
        /// <param name="onDisconnectCallback">Функция, вызываемая при отключении от сети.</param>
        public NetworkMonitor(Func<Task> onDisconnectCallback)
        {
            _onDisconnectCallback = onDisconnectCallback;
        }

        /// <summary>
        /// Запускает мониторинг сети, отслеживая изменения в подключении.
        /// </summary>
        public void StartMonitoring()
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkChanged;

            _currentSsid = GetCurrentWifiSsid();
            Debug.WriteLine($"✅ Мониторинг сети запущен. Текущая сеть: {_currentSsid}");
        }

        /// <summary>
        /// Обрабатывает изменения в сети и вызывает соответствующие действия.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="args">Аргументы события.</param>
        private async void OnNetworkChanged(object? sender, EventArgs args)
        {
            await Task.Delay(1000);

            bool isNowConnected = IsConnectedToCurrentNetwork();

            if (!isNowConnected && _isConnected)
            {
                _isConnected = false;
                Debug.WriteLine("❌ Отключение от сети! Вызываем колбэк...");
                await _onDisconnectCallback();
            }
            else if (isNowConnected && !_isConnected)
            {
                _isConnected = true;
                Debug.WriteLine("✅ Интернет восстановлен.");
            }
        }

        /// <summary>
        /// Останавливает мониторинг сети и отписывается от событий.
        /// </summary>
        public void Dispose()
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
            Debug.WriteLine("🛑 Мониторинг сети остановлен.");
        }

        /// <summary>
        /// Проверяет текущее состояние подключения к сети.
        /// </summary>
        /// <returns>Возвращает true, если устройство подключено к Wi-Fi, иначе false.</returns>
        private static bool IsConnectedToCurrentNetwork()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Any(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                                nic.OperationalStatus == OperationalStatus.Up &&
                                nic.GetIPProperties().UnicastAddresses.Count > 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Ошибка при проверке сети: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получает SSID текущей сети Wi-Fi.
        /// </summary>
        /// <returns>SSID сети или null, если получить не удалось.</returns>
        private static string? GetCurrentWifiSsid()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                                  nic.OperationalStatus == OperationalStatus.Up)
                    .Select(nic => nic.Description)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Ошибка при получении SSID: {ex.Message}");
                return null;
            }
        }
    }
}
