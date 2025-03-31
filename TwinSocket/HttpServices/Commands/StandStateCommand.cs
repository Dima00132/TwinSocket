using RelayProtection.Models;
using RelayProtection.Services.HttpServices.Abstract;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RelayProtection.Services.HttpServices.Commands
{

    /// <summary>
    /// Команда, представляющая состояние системы, включая статус учителя и ученика.
    /// </summary>
    public class StandConnectCommand : IWebCommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string CommandName => nameof(StandStateCommand);

        /// <summary>
        /// Номер стенда.
        /// </summary>
        public int StandNumber { get; set; }

        /// <summary>
        /// Текущее состояние стенда, отображающее статус взаимодействия между учителем и учеником.
        /// </summary>
        public StandState StandState { get; set; }

        public bool IsConnect { get; set; }

        /// <summary>
        /// Конструктор без параметров для создания пустого объекта команды.
        /// Этот конструктор используется для десериализации объекта команды из строки.
        /// </summary>
        public StandConnectCommand()
        {
        }

        /// <summary>
        /// Конструктор для создания команды с заданным номером стенда и состоянием.
        /// </summary>
        /// <param name="standNumber">Номер стенда.</param>
        /// <param name="jointStatus">Статус системы (учитель и ученик).</param>
        public StandConnectCommand(StandState _standState, bool isConnect)
        {
            StandNumber = _standState.StandNumber;
            StandState = _standState;
            IsConnect = isConnect;
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



    /// <summary>
    /// Команда, представляющая состояние системы, включая статус учителя и ученика.
    /// </summary>
    public class StandStateCommand : IWebCommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string CommandName => nameof(StandStateCommand);

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
        public StandStateCommand()
        {
        }

        /// <summary>
        /// Конструктор для создания команды с заданным номером стенда и состоянием.
        /// </summary>
        /// <param name="standNumber">Номер стенда.</param>
        /// <param name="jointStatus">Статус системы (учитель и ученик).</param>
        public StandStateCommand(StandState _standState)
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
