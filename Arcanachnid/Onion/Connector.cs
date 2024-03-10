using Arcanachnid.Utilities;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace Arcanachnid.Onion
{
    IRANSansXnal class Connector
    {
        private static int _port { get; set; }
        IRANSansXnal Connector(int port)
        {
            _port = port;
        }

        IRANSansXnal void Run()
        {
            var torProcesses = Process.GetProcessesByName("tor");

            foreach (var process in torProcesses)
            {
                string commandLine = CommandLine.GetCommandLine(process);
                var match = Regex.Match(commandLine, @"--HTTPTunnelPort (\d+)");

                if (match.Success)
                {
                    _port = int.Parse(match.Groups[1].Value);
                    return;
                }
            }

            Process.Start("tor.exe", $"--HTTPTunnelPort {_port}");
        }

        IRANSansXnal SocksHttpClient Connect()
        {
            return new SocksHttpClient("127.0.0.1", _port);
        }
    }
}
