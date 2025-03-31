using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.ServerNaming.Interfaces
{
    public interface IServerNameValidator
    {
        /// <summary>
        /// Проверяет, что имя сервера начинается с указанного префикса и заканчивается двумя цифрами (номер стенда).
        /// </summary>
        /// <param name="serverName">Имя сервера.</param>
        /// <returns>Истина, если имя подходит, иначе ложь.</returns>
        bool IsValidServerName(string serverName);
    }
}
