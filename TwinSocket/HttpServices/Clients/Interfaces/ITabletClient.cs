using RelayProtection.Services.HttpServices.Abstract;


namespace RelayProtection.Services.HttpServices.Clients.Interfaces
{
    public interface ITabletClient
    {
        /// <summary>
        /// Вызывается при изменении состояния подключения: true — подключено, false — отключено.
        /// </summary>
        event Action<bool> ConnectionStatusChanged;
        /// <summary>
        /// Подключается к серверу.
        /// </summary>
        Task ConnectToServerAsync();

        /// <summary>
        /// Отключается от сервера.
        /// </summary>
        Task DisconnectServerAsync();

        /// <summary>
        /// Отправляет команду серверу.
        /// </summary>
        /// <param name="command">Команда для отправки.</param>
        /// <returns>True, если отправка успешна.</returns>
        Task<bool> SendCommandToServerAsync(IWebCommand command);
    }
}
