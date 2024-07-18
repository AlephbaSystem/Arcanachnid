using MihaZupan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Onion
{
    public class SocksHttpClient
    {
        private readonly HttpClient _httpClient;

        public SocksHttpClient(string proxyHost, int proxyPort)
        {
            var proxy = new HttpToSocks5Proxy(proxyHost, proxyPort);
            var httpClientHandler = new HttpClientHandler { Proxy = proxy };

            _httpClient = new HttpClient(httpClientHandler);
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            return await _httpClient.GetStringAsync(requestUri);
        }
    }
}
