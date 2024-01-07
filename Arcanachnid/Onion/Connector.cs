using System.Diagnostics;
using System.Net;

namespace Arcanachnid.Onion
{
    internal class Connector
    {
        private static int _port { get; set; }
        internal Connector(int port)
        {
            _port = port;
        }
        public static void Run()
        {
            Process.Start("tor.exe", $"--HTTPTunnelPort {_port}");
        }

        public static HttpClient Connect()
        {
            var proxy = new WebProxy($"127.0.0.1:{_port}");
            proxy.UseDefaultCredentials = true;

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
            };

            return new HttpClient(httpClientHandler);
        }
    }
}
