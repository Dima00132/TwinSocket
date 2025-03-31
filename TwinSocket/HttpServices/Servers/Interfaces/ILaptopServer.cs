using System;
using System.Threading;
using System.Threading.Tasks;
using RelayProtection.Services.HttpServices.Abstract;
using RelayProtection.Services.HttpServices.Commands;

namespace RelayProtection.Services.HttpServices.Servers.Interfaces
{
    /// <summary>
    /// Интерфейс для взаимодействия с сервером на ноутбуке, который обрабатывает HTTP- и WebSocket-запросы.
    /// </summary>
    public interface ILaptopServer : IDisposable
    {
        /// <summary>
        /// Событие, которое возникает при завершении соединения с Wi-Fi.
        /// </summary>
        event Action<bool> ConnectionStatusChanged;

        /// <summary>
        /// Событие, которое возникает при изменении статуса сервера (запущен или остановлен).
        /// </summary>
        event Action<bool>? ServerStatusChanged;

        /// <summary>
        /// Реестр обработчиков команд.
        /// </summary>
        CommandHandlerRegistry CommandHandlerRegistry { get; }

        /// <summary>
        /// Запускает сервер и начинает обработку запросов.
        /// </summary>
        /// <returns>Задача, которая завершится, когда сервер начнёт обработку запросов.</returns>
        Task StartAsync(CancellationTokenSource cancellationToken);

        /// <summary>
        /// Отправляет команду клиенту через WebSocket.
        /// </summary>
        /// <param name="command">Команда для отправки клиенту.</param>
        /// <returns>Задача, которая завершится, когда команда будет отправлена.</returns>
        Task<bool> SendCommandToClientAsync(IWebCommand command, CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// Останавливает сервер.
        /// </summary>
        void Stop();
    }
}
