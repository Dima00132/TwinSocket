using System.Collections.ObjectModel;
using System.Text.Json;
using RelayProtection.Models;
using RelayProtection.Services.HttpServices.Abstract;

namespace RelayProtection.Services.HttpServices.Commands
{
    /// <summary>
    /// Команда, представляющая данные датчиков системы.
    /// </summary>
    public class SensorCommand : IWebCommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string CommandName => nameof(SensorCommand);

        /// <summary>
        /// Номер стенда.
        /// </summary>
        public int StandNumber { get; set; }

        /// <summary>
        /// Текущее состояние стенда, отображающее статус взаимодействия между учителем и учеником.
        /// </summary>
        public StandState StandState { get; set; }

        /// <summary>
        /// Текущие данные, полученные с датчиков системы.
        /// </summary>
        public ObservableCollection<SensorDisplay> SensorDisplays { get; set; }

        /// <summary>
        /// Конструктор без параметров для создания пустого объекта команды.
        /// Этот конструктор используется для десериализации объекта команды из строки.
        /// </summary>
        public SensorCommand() { }

        /// <summary>
        /// Конструктор для создания команды с заданным номером стенда и данными датчиков.
        /// </summary>
        /// <param name="sensorData">Данные, полученные с датчиков.</param>
        public SensorCommand(StandState standState, ObservableCollection<SensorDisplay> sensorDisplays)
        {
            StandNumber = standState.StandNumber;
            StandState = standState;
            SensorDisplays = sensorDisplays;
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
