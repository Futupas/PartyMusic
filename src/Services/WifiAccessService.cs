using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace PartyMusic.Services
{
    public class WifiAccessService
    {
        private string ExecuteCommand(string command, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();
            //Console.WriteLine(output);

            return output;
        }
        public string GetNetworkName()
        {
            string output = ExecuteCommand("netsh", "wlan show interfaces");
            output = output.Trim();
            string pattern = @"SSID\s+:\s+(.*)";
            Match match = Regex.Match(output, pattern);
            if (match.Success)
            {
                //Console.WriteLine(output);
                return match.Groups[1].Value.Trim();
            }
            else
            {
                return string.Empty;
            }
        }

        public string WifiConnectionDataRetriever(string networkName)
        {
            if (!string.IsNullOrEmpty(networkName))
            {
                string dirtyOutput = ExecuteCommand("netsh", $"wlan show profile {networkName.Trim()} key = clear");
                string pattern = @"Key Content\s+:\s+(.*)";
                Match match = Regex.Match(dirtyOutput, pattern);
                string output = match.Groups[1].Value;

                string toReturn = networkName + "," + output.Trim();
                Console.WriteLine(toReturn);
                return toReturn;
            }
            else
            {
                Console.WriteLine("Could not retrieve network name!");
                return String.Empty;
            }
        }
    }
}
