using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Onion
{
    public class SocksWebRequest : WebRequest
    {
        // Implementation that uses a ProxySocket
    }

    public class SocksWebResponse : WebResponse
    {
        // Implementation that reads data from a ProxySocket
    }

    public class SocksWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = new SocksWebRequest(address);
            // Configure the request for SOCKS proxy
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            // Get the WebResponse from the SocksWebRequest
            return new SocksWebResponse(request);
        }
    }

}
