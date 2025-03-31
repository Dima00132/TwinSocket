using System;
using System.Diagnostics;

namespace RelayProtection.Services.HttpServices.Networking
{
    public static class PowerShellLauncher
    {
        public static void DisableActiveProbing()
        {
            string command = "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NlaSvc\\Parameters\\Internet' -Name 'EnableActiveProbing' -Value 0 -Force";

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{command}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
