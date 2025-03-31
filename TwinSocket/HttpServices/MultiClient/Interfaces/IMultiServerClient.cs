using RelayProtection.Services.HttpServices.Abstract;
using RelayProtection.Services.HttpServices.Commands;
using RelayProtection.Services.HttpServices.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.MultiClient.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с многосерверным клиентом, поддерживающим подключение, обнаружение серверов, отправку команд и управление состоянием.
    /// </summary>
    /// <typeparam name="T">Тип данных, используемых для уведомлений о событиях.</typeparam>
    public interface IMultiServerClient<T> : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Событие, которое возникает при обнаружении нового сервера.
        /// </summary>
        /// <param name="serverName">Имя сервера.</param>
        /// <param name="serverAddress">Адрес сервера.</param>
        event Action<string, string>? OnServerDiscovered;

        /// <summary>
        /// Объект для управления событиями уведомлений, связанными с серверами.
        /// </summary>
        INotificationEvents<T> NotificationEvents { get; }

        /// <summary>
        /// Регистратор команд для обработки и выполнения команд на серверах.
        /// </summary>
        CommandHandlerRegistry CommandHandlerRegistry { get; }

        /// <summary>
        /// Проверяет, подключён ли клиент к указанному серверу.
        /// </summary>
        /// <param name="serverKey">Ключ сервера для проверки подключения.</param>
        /// <returns>True, если подключение к серверу установлено; иначе False.</returns>
        bool IsConnectedToServer(string serverKey);

        /// <summary>
        /// Асинхронный метод для начала процесса обнаружения серверов и установления соединений.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task StartDiscoveryAndConnectAsync();

        /// <summary>
        /// Асинхронный метод для подключения к конкретному серверу по имени и адресу.
        /// </summary>
        /// <param name="serverName">Имя сервера.</param>
        /// <param name="serverAddress">Адрес сервера.</param>
        /// <returns>Задача, представляющая асинхронную операцию подключения.</returns>
        Task ConnectToServerAsync(string serverName, string serverAddress);

        /// <summary>
        /// Асинхронный метод для отключения от сервера по ключу сервера.
        /// </summary>
        /// <param name="serverKey">Ключ сервера для отключения.</param>
        /// <returns>Задача, представляющая асинхронную операцию отключения.</returns>
        Task DisconnectServerAsync(string serverKey);

        /// <summary>
        /// Асинхронный метод для отправки команды на сервер.
        /// </summary>
        /// <param name="serverKey">Ключ сервера, на который отправляется команда.</param>
        /// <param name="command">Команда для выполнения на сервере.</param>
        /// <returns>Задача, представляющая асинхронную операцию отправки команды.</returns>
        Task<bool> SendCommandToServerAsync(string serverKey, IWebCommand command);
    }
}
