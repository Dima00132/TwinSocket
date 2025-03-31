using RelayProtection.Models;
using RelayProtection.Services.HttpServices.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.Commands
{
    /// <summary>
    /// Команда, представляющая восстановленное состояние системы, включая статус учителя и ученика.
    /// </summary>
    public class RestoredStateCommand : IWebCommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string CommandName => nameof(RestoredStateCommand);

        /// <summary>
        /// Номер стенда.
        /// </summary>
        public int StandNumber { get; set; }

        /// <summary>
        /// Восстановленное состояние стенда, отображающее статус взаимодействия между учителем и учеником.
        /// </summary>
        public StandState RestoredState { get; set; }

        /// <summary>
        /// Конструктор без параметров для создания пустого объекта команды.
        /// Этот конструктор используется для десериализации объекта команды из строки.
        /// </summary>
        public RestoredStateCommand()
        {
        }

        /// <summary>
        /// Конструктор для создания команды с заданным номером стенда и восстановленным состоянием.
        /// </summary>
        /// <param name="restoredState">Состояние системы (учитель и ученик).</param>
        public RestoredStateCommand(StandState restoredState)
        {
            StandNumber = restoredState.StandNumber;
            RestoredState = restoredState;
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
