using RelayProtection.Services.HttpServices.ServerNaming.Interfaces;
using RelayProtection.Services.HttpServices.ServerNaming.Models;
using System;
using System.Linq;

namespace RelayProtection.Services.HttpServices.ServerNaming.Validators
{
    public class DataExtractor(bool skipValidationForTesting = false) : IDataExtractor<ServerInfo>
    {
        //TODO необходимо для тестирования
        private readonly bool _skipValidationForTesting = skipValidationForTesting;

        public ServerInfo ExtractData(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Входное значение не может быть пустым или равно null.", nameof(input));
            }

            //TODO необходимо для тестирования 
            if (_skipValidationForTesting)
            {
                return new ServerInfo(input, 12);
            }

            if (!HasValidSuffix(input))
            {
                throw new ArgumentException($"Входное значение '{input}' не соответствует ожидаемому формату.", nameof(input));
            }

            return CreateItemFromInput(input);
        }

        private bool HasValidSuffix(string input)
        {
            string suffixPart = ExtractNumberFromEnd(input);

            return int.TryParse(suffixPart, out _);
        }

        private string ExtractNumberFromEnd(string input)
        {
            string numericSuffix = new string(input.Reverse().TakeWhile(char.IsDigit).ToArray());
            return new string(numericSuffix.Reverse().ToArray());
        }

        private ServerInfo CreateItemFromInput(string input)
        {
            string numericSuffix = ExtractNumberFromEnd(input);

            if (string.IsNullOrEmpty(numericSuffix))
            {
                throw new ArgumentException($"Входное значение '{input}' не имеет допустимого суффикса с числом.", nameof(input));
            }

            int number = int.Parse(numericSuffix);
            return new ServerInfo(input, number);
        }
    }
}
