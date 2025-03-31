using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelayProtection.Services.HttpServices.ServerNaming.Models
{
    public record ServerInfo(string ServerName, int StandNumber)
    {
        public string ServerName { get; init; } = ServerName.ToLowerInvariant();
    }
}
