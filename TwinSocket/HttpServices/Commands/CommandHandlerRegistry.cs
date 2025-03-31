using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using RelayProtection.Services.HttpServices.Abstract;
using RelayProtection.Services.HttpServices.ServerNaming.Interfaces;
using RelayProtection.Services.HttpServices.ServerNaming.Models;

namespace RelayProtection.Services.HttpServices.Commands
{
    public class CommandHandlerRegistry
    {
        private const string COMMAND_NAME_PROPERTY = "CommandName";
        private readonly Dictionary<Type, Action<IWebCommand>> _commandHandlers;
        private readonly Dictionary<Type, Action<ServerInfo, IWebCommand>> _serverCommandHandlers;
        private readonly Dictionary<string, Type> _commandTypes;
        readonly IDataExtractor<ServerInfo> _standNumberExtractor;

        public CommandHandlerRegistry(IDataExtractor<ServerInfo> standNumberExtractor)
        {
            _commandHandlers = new Dictionary<Type, Action<IWebCommand>>();
            _serverCommandHandlers = new Dictionary<Type, Action<ServerInfo, IWebCommand>>();
            _commandTypes = new Dictionary<string, Type>();
            _standNumberExtractor = standNumberExtractor;
            RegisterCommandTypesFromAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Регистрирует обработчик для команды, который выполняет действие.
        /// </summary>
        /// <typeparam name="TCommand">Тип команды.</typeparam>
        /// <param name="handler">Действие для обработки команды.</param>
        public void RegisterCommandHandler<TCommand>(Action<TCommand> handler) where TCommand : IWebCommand
        {
            Type commandType = typeof(TCommand);

            _commandHandlers[commandType] = command => handler((TCommand)command);
        }


        /// <summary>
        /// Регистрирует обработчик для команды с учетом имени сервера.
        /// </summary>
        /// <typeparam name="TCommand">Тип команды.</typeparam>
        /// <param name="handler">Действие для обработки команды.</param>
        public void RegisterServerCommandHandler<TCommand>(Action<ServerInfo, TCommand> handler) where TCommand : IWebCommand
        {
            Type commandType = typeof(TCommand);
            _serverCommandHandlers[commandType] = (serverName, command) => handler(serverName, (TCommand)command);
        }

        /// <summary>
        /// Выполняет обработку команды, вызывая зарегистрированный обработчик.
        /// </summary>
        /// <param name="command">Команда для обработки.</param>
        public void ExecuteCommand(IWebCommand command)
        {
            Type commandType = command.GetType();

            if (_commandHandlers.TryGetValue(commandType, out Action<IWebCommand> handler))
            {
                handler(command);
            }
        }

        /// <summary>
        /// Выполняет обработку команды с учетом имени сервера, вызывая зарегистрированный обработчик.
        /// </summary>
        /// <param name="serverName">Имя сервера.</param>
        /// <param name="command">Команда для обработки.</param>
        public void ExecuteCommand(string serverName, IWebCommand command)
        {
            Type commandType = command.GetType();

            if (_serverCommandHandlers.TryGetValue(commandType, out Action<ServerInfo, IWebCommand> handler))
            {
                handler(_standNumberExtractor.ExtractData(serverName), command);
            }
        }

        /// <summary>
        /// Асинхронно выполняет обработку команды, вызывая зарегистрированный обработчик.
        /// </summary>
        /// <param name="command">Команда для обработки.</param>
        public async Task ExecuteCommandAsync(IWebCommand command)
        {
            Type commandType = command.GetType();

            if (_commandHandlers.TryGetValue(commandType, out Action<IWebCommand>? handler) && handler != null)
            {
                await Task.Run(() => handler(command));
            }
        }

        /// <summary>
        /// Извлекает тип команды из JSON, использует кэширование и уменьшает нагрузку на рефлексию.
        /// </summary>
        public Type ExtractCommandType(string json)
        {
            JsonDocument jsonDocument = JsonDocument.Parse(json);

            if (!jsonDocument.RootElement.TryGetProperty(COMMAND_NAME_PROPERTY, out JsonElement commandNameElement))
            {
                throw new KeyNotFoundException($"The key '{COMMAND_NAME_PROPERTY}' was not found in the JSON document.");
            }

            string commandName = commandNameElement.GetString();
            if (string.IsNullOrEmpty(commandName))
            {
                throw new InvalidOperationException($"The '{COMMAND_NAME_PROPERTY}' property is null or empty.");
            }

            if (_commandTypes.TryGetValue(commandName, out Type commandType))
            {
                return commandType;
            }

            throw new InvalidOperationException($"Unknown command type: {commandName}");
        }

        /// <summary>
        /// Регистрирует все классы, реализующие интерфейс IHttpCommand из сборки.
        /// </summary>
        /// <param name="assembly">Сборка, из которой будут извлечены классы.</param>
        private void RegisterCommandTypesFromAssembly(Assembly assembly)
        {
            List<Type> commandTypes = assembly
                .GetTypes()
                .Where(t => typeof(IWebCommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (Type commandType in commandTypes)
            {
                _commandHandlers[commandType] = command => { };
                _serverCommandHandlers[commandType] = (serverName, command) => { };
                _commandTypes[commandType.Name] = commandType;
            }
        }
    }
}
