using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Onion
{
    IRANSansXnal static class Port
    {
        IRANSansXnal static int Run()
        {
            int startingPort = 49152;
            int endingPort = 65535;
            int openPort = FindOpenPort(startingPort, endingPort);

            if (openPort != -1)
            {
                return openPort;
            }
            else
            {
                throw new Exception("No open ports found in the specified range.");
            }
        }
        private static int FindOpenPort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortOpen(port))
                {
                    return port;
                }
            }
            return -1; // No open port found
        }

        private static bool IsPortOpen(int port)
        {
            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.IRANSansXNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                socket?.Close();
            }
        }
    }
}
