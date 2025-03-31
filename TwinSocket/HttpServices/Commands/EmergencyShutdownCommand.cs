using RelayProtection.Models;
using RelayProtection.Services.HttpServices.Abstract;
using System.Text.Json;


namespace RelayProtection.Services.HttpServices.Commands
{
    public class EmergencyShutdownCommand : IWebCommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string CommandName => nameof(EmergencyShutdownCommand);

        /// <summary>
        /// Номер стенда.
        /// </summary>
        public int StandNumber { get; set; }
   
        /// <summary>
        /// Текущее состояние стенда, отображающее статус взаимодействия между учителем и учеником.
        /// </summary>
        public StandState StandState { get; set; }

        /// <summary>
        /// Конструктор без параметров для создания пустого объекта команды.
        /// Этот конструктор используется для десериализации объекта команды из строки.
        /// </summary>
        public EmergencyShutdownCommand()
        {
        }

        /// <summary>
        /// Конструктор для создания команды с данными и номером стенда.
        /// </summary>
        public EmergencyShutdownCommand(StandState _standState)
        {
            StandNumber = _standState.StandNumber;
            StandState = _standState;
        }

        /// <summary>
        /// Преобразует объект команды в строку для передачи по сети.
        /// </summary>
        /// <returns>Строка в формате JSON.</returns>
        public string ToSerializedString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
