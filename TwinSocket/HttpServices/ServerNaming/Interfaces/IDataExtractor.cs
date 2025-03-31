using RelayProtection.Services.HttpServices.ServerNaming.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.ServerNaming.Interfaces
{
    public interface IDataExtractor<TOutput>
    {
        /// <summary>
        /// Извлекает данные из строки на основе определённых правил.
        /// В зависимости от типа TOutput, может извлекать различные данные, такие как номер стенда или другие значения.
        /// </summary>
        /// <param name="serverName">Строка, из которой извлекаются данные. Например, имя сервера.</param>
        /// <returns>Возвращает объект типа TOutput, который содержит извлечённые данные.</returns>
        TOutput ExtractData(string serverName);
    }
}
