using System;
using System.Diagnostics;

namespace RelayProtection.Services.HttpServices.Networking
{
    public static class AdminRightsManager
    {
        private const string CMD_EXECUTABLE = "cmd";
        private const string VERB_RUNAS = "runas";
        private const int EXIT_CODE_SUCCESS = 0;
        private const uint ERROR_ACCESS_DENIED = 0x80004005;

        public static void EnsureAdminRights(string[] urls, string firewallRuleName, ulong httpPort)
        {
            try
            {
                foreach (string url in urls)
                {
                    string output = ExecuteProcessAndGetOutput(
                        CMD_EXECUTABLE,
                        $"/c netsh http show urlacl url={url}",
                        useShellExecute: false,
                        redirectOutput: true
                    );

                    if (!output.Contains(url))
                    {
                        ExecuteProcess(
                            CMD_EXECUTABLE,
                            $"/c netsh http add urlacl url={url} user={Environment.UserName}",
                            useShellExecute: true,
                            verb: VERB_RUNAS
                        );
                    }
                }

                EnsureFirewallRuleExists(firewallRuleName, httpPort);
            }
            catch (System.ComponentModel.Win32Exception ex) when ((uint)ex.ErrorCode == ERROR_ACCESS_DENIED)
            {
                Debug.WriteLine("Пользователь отказался предоставить права администратора. Приложение будет закрыто.");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось установить разрешения для URL: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void EnsureFirewallRuleExists(string ruleName, ulong localPort)
        {
            string ruleOutput = ExecuteProcessAndGetOutput(
                CMD_EXECUTABLE,
                $"/c netsh advfirewall firewall show rule name=\"{ruleName}\"",
                useShellExecute: false,
                redirectOutput: true
            );

            if (!ruleOutput.Contains(ruleName))
            {
                ExecuteProcess(
                    CMD_EXECUTABLE,
                    $"/c netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={localPort}",
                    useShellExecute: true,
                    verb: VERB_RUNAS
                );

                ExecuteProcess(
                    CMD_EXECUTABLE,
                    $"/c netsh advfirewall firewall set rule name=\"{ruleName}\" new profile=domain,private,public",
                    useShellExecute: true,
                    verb: VERB_RUNAS
                );
            }
        }

        private static string ExecuteProcessAndGetOutput(
            string fileName,
            string arguments,
            bool useShellExecute,
            bool redirectOutput
        )
        {
            ProcessStartInfo processInfo = new(fileName, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = redirectOutput,
            };

            using Process process = Process.Start(processInfo);
            string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();
            return output;
        }

        private static void ExecuteProcess(string fileName, string arguments, bool useShellExecute, string verb = null)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = useShellExecute,
                Verb = verb,
            };

            using Process process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process?.ExitCode != EXIT_CODE_SUCCESS)
            {
                Debug.WriteLine($"Процесс завершился с кодом ошибки {process.ExitCode}.");
                Environment.Exit(1);
            }
        }
    }
}
