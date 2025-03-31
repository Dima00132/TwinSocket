using RelayProtection.Services.HttpServices.ServerNaming.Interfaces;
using System;


namespace RelayProtection.Services.HttpServices.ServerNaming.Validators
{
    public class ServerNameValidator(string expectedPrefix, bool skipValidationForTesting = false) : IServerNameValidator
    {
        private readonly string _expectedPrefix = expectedPrefix;
        private readonly bool _skipValidationForTesting = skipValidationForTesting;

        public bool IsValidServerName(string serverName)
        {
            if (_skipValidationForTesting) return true;

            return !string.IsNullOrEmpty(serverName) &&
                   serverName.StartsWith(_expectedPrefix, StringComparison.OrdinalIgnoreCase) &&
                   HasValidStandNumber(serverName);
        }

        private bool HasValidStandNumber(string serverName)
        {
            string standNumberPart = serverName.Substring(_expectedPrefix.Length);
            return int.TryParse(standNumberPart, out _);
        }
    }
}
