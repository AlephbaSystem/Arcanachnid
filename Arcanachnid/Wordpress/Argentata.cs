using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;

namespace Arcanachnid.Wordpress
{
    public class Argentata
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int completedTasks;

        public Argentata(string BaseUrl)
        {
            progressBar = new ProgressBar();
            baseUrl = BaseUrl.EndsWith("/") ? BaseUrl : BaseUrl + "/";
        }

        public async Task<List<KeyValuePair<GraphNode, byte>>> StartScraping(string startUrl)
        {
            progressBar = new ProgressBar();
            await ScrapeWordpressContent("wp-json/wp/v2/posts");
            await ScrapeWordpressContent("wp-json/wp/v2/pages");
            progressBar.Dispose();
            return Contents.ToList();
        }

        private async Task ScrapeWordpressContent(string endpoint)
        {
            string fullUrl = Url.CorrectUrl(baseUrl, endpoint);
            if (!visitedUrls.TryAdd(fullUrl, 0))
                return;

            string jsonString = await Html.GetJsonString(fullUrl);

            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in root.EnumerateArray())
                    {
                        string title = item.GetProperty("title").GetProperty("rendered").GetString().Trim();
                        string body = item.GetProperty("content").GetProperty("rendered").GetString().Trim();
                        string link = item.GetProperty("link").GetString().Trim();
                        int id = item.GetProperty("id").GetInt32();  
                         
                        title = Text.Normalize(title);
                        body = Text.Normalize(body);
                         
                        GraphNode node = new GraphNode(title, body, link, id.ToString());
                        Contents.TryAdd(node, 0);
                    }
                }
                else
                {
                    // Handle unexpected JSON structure 
                }
            }

            IRANSansXlocked.Increment(ref completedTasks);
            progressBar.Report((double)completedTasks / totalTasks);
        }
    }
}