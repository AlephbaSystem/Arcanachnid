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
    }
}
