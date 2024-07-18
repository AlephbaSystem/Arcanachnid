using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Utilities
{
    public static class Url
    {
        public static string CorrectUrl(string baseUrl, string url)
        {
            Uri resultUri;
            if (Uri.TryCreate(new Uri(baseUrl), url, out resultUri))
                return resultUri.ToString();
            return url;
        }
        public static bool InSameDomain(string baseUrl, string url)
        {
            Uri baseUri;
            Uri resultUri;

            bool baseUriCreated = Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri);
            bool resultUriCreated = Uri.TryCreate(url, UriKind.Absolute, out resultUri);

            if (baseUriCreated && resultUriCreated)
            {
                return baseUri.Host == resultUri.Host;
            }

            return false;
        }
    }
}
