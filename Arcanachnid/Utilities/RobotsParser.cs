using Arcanachnid.Models;

namespace Arcanachnid.Utilities
{
    public class RobotsParser
    {
        private HttpClient _httpClient;
        public RobotsParser(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<RobotsFile> FromUriAsync(Uri uri)
        {
            string robotsTxtUrl = uri.AbsoluteUri.TrimEnd('/') + "/robots.txt";
            HttpResponseMessage response = await _httpClient.GetAsync(robotsTxtUrl);
            string content = await response.Content.ReadAsStringAsync();

            RobotsFile robotsFile = ParseRobotsTxt(content);

            return robotsFile;
        }

        private RobotsFile ParseRobotsTxt(string content)
        {
            try
            {
                RobotsFile robotsFile = new RobotsFile();
                StringReader stringReader = new StringReader(content);

                string line;
                string currentUserAgent = null;

                while ((line = stringReader.ReadLine()) != null)
                {

                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(new char[] { ':' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string directive = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();

                    switch (directive)
                    {
                        case "user-agent":
                            currentUserAgent = value;
                            break;
                        case "disallow":
                            robotsFile.AddRule(currentUserAgent, value, allow: false);
                            break;
                        case "allow":
                            robotsFile.AddRule(currentUserAgent, value, allow: true);
                            break;
                        case "crawl-delay":
                            if (int.TryParse(value, out int seconds))
                            {
                                robotsFile.SetCrawlDelay(currentUserAgent, TimeSpan.FromSeconds(seconds));
                            }
                            break;
                        default:
                            // Unknown directive, can be safely ignored
                            break;
                    }
                }
                return robotsFile;
            }
            catch (Exception)
            {
                return new RobotsFile();
            }
        }
    }

}
