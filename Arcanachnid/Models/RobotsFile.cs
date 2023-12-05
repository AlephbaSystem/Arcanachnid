using System.Text.RegularExpressions;

namespace Arcanachnid.Models
{
    public class RobotsFile
    {
        public List<string> UserAgents { get; set; }
        public List<string> DisallowedPaths { get; set; }
        public List<string> AllowedPaths { get; set; }
        public TimeSpan? CrawlDelay { get; set; }

        private Dictionary<string, List<string>> Disallows = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> Allows = new Dictionary<string, List<string>>();
        private Dictionary<string, TimeSpan> CrawlDelays = new Dictionary<string, TimeSpan>();
        public void AddRule(string userAgent, string path, bool allow)
        {
            if (allow)
            {
                if (!Allows.ContainsKey(userAgent))
                {
                    Allows[userAgent] = new List<string>();
                }
                Allows[userAgent].Add(path);
            }
            else
            {
                if (!Disallows.ContainsKey(userAgent))
                {
                    Disallows[userAgent] = new List<string>();
                }
                Disallows[userAgent].Add(path);
            }
        }

        public void SetCrawlDelay(string userAgent, TimeSpan delay)
        {
            CrawlDelays[userAgent] = delay;
        }

        public bool IsAllowed(string path, string userAgent)
        {
            var agentsToCheck = new List<string>{ userAgent, "*" };

            foreach (var agent in agentsToCheck)
            {
                if (Disallows.TryGetValue(agent, out var disallowPaths))
                {
                    if (disallowPaths.Any(disallowPath => MatchesPath(path, disallowPath)))
                    {
                        return false;
                    }
                }
                if (Allows.TryGetValue(agent, out var allowPaths))
                {
                    if (allowPaths.Any(allowPath => MatchesPath(path, allowPath)))
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        private bool MatchesPath(string path, string rulePath)
        {
            string regexPattern = "^" + Regex.Escape(rulePath)
            .Replace("\\*", ".*")
            .Replace("\\$", "$") + "$";
            return Regex.IsMatch(path, regexPattern);
        }
    }

}
