namespace RelayProtection.Services.HttpServices.Servers.Implementations
{
    public class LaptopServerOptions
    {
        public ushort HttpPort { get; set; } = 8080;

        /// <summary>
        /// Имя сервера, к которому будет выполняться подключение.
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        public ushort ServerDiscoveryPort { get; set; } = 15000;
        public int BufferSize { get; set; } = 4096;
        public string FirewallRuleName { get; set; } = "Allow TCP {0}";
        public int WebSocketTimeoutMs { get; set; } = 30000;
        public int ReconnectDelayMs { get; set; } = 1000;
    }

}
