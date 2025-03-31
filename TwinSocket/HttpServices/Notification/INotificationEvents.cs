using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.Notification
{
    /// <summary>
    /// Интерфейс для обработки событий уведомлений о статусе соединения и отключении сервера.
    /// </summary>
    /// <typeparam name="T">Тип данных, передаваемых с событиями (например, информация о сервере).</typeparam>
    public interface INotificationEvents<T>
    {
        /// <summary>
        /// Событие, которое происходит при изменении статуса соединения.
        /// </summary>
        event Action<T> OnServerConnected;

        /// <summary>
        /// Событие, которое происходит при отключении сервера.
        /// </summary>
        event Action<T>? OnServerDisconnected;

        /// <summary>
        /// Метод для уведомления об изменении статуса соединения.
        /// </summary>
        /// <param name="status">Статус соединения, который нужно передать.</param>
        void NotifyServerConnected(string status);

        /// <summary>
        /// Метод для уведомления о событии отключения сервера.
        /// </summary>
        /// <param name="serverInfo">Информация о сервере, который был отключен.</param>
        void NotifyServerDisconnected(string serverInfo);
    }
}
