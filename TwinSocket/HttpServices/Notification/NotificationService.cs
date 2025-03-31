using RelayProtection.Services.HttpServices.ServerNaming.Interfaces;
using RelayProtection.Services.HttpServices.ServerNaming.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.Notification
{
    public class NotificationService : INotificationEvents<ServerInfo>
    {
        public event Action<ServerInfo> OnServerConnected;
        public event Action<ServerInfo> OnServerDisconnected;

        private readonly IDataExtractor<ServerInfo> _standNumberExtractor;
        public NotificationService(IDataExtractor<ServerInfo> standNumberExtractor)
        {
            _standNumberExtractor = standNumberExtractor;
        }

        public void NotifyServerConnected(string serverInfo)
        {
            OnServerConnected?.Invoke(_standNumberExtractor.ExtractData(serverInfo));
        }

        public void NotifyServerDisconnected(string serverInfo)
        {
            OnServerDisconnected?.Invoke(_standNumberExtractor.ExtractData(serverInfo));
        }
    }
}
