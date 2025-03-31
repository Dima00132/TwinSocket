using RelayProtection.Enums;
using RelayProtection.Models;
using RelayProtection.Services.HttpServices.Abstract;
using System.Text.Json;

namespace RelayProtection.Services.HttpServices.Commands
{
    public class NotificationCommand : IWebCommand
    {
        public string CommandName => nameof(NotificationCommand);

        public int StandNumber { get; set; }

        public NotificationType NotificationType { get; set; }

        public StandState StandSrate { get; set; }

        public NotificationCommand() { }

        public NotificationCommand(StandState standState, NotificationType notificationType) 
        { 
            StandNumber = standState.StandNumber;
            NotificationType = notificationType;
            StandSrate = standState;
        }

        /// <summary>
        /// Преобразует объект команды в строку для передачи по сети.
        /// </summary>
        /// <returns>Строка в формате JSON.</returns>
        public string ToSerializedString()
        {
            // Сериализация команды
            return JsonSerializer.Serialize(this);
        }
    }   
}
