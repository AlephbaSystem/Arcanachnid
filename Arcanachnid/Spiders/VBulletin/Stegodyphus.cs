using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System.Collections.Concurrent;

namespace Arcanachnid.Spiders.VBulletin
{
    public class Stegodyphus
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int completedTasks;

        public Stegodyphus(string BaseUrl)
        {
            baseUrl = BaseUrl;
            progressBar = new ProgressBar();
        }

        public async Task<List<KeyValuePair<GraphNode, byte>>> StartScraping(string startUrl)
        {
            progressBar = new ProgressBar();
            await ScrapeForumPages(startUrl);
            progressBar.Dispose();
            return Contents.ToList();
        }

        private async Task ScrapeForumPages(string url)
        {
            if (visitedUrls.ContainsKey(url))
                return;

            visitedUrls.TryAdd(url, 0);

            string docUrl = Url.CorrectUrl(baseUrl, url);
            HtmlDocument doc = await Html.GetHtmlDocument(docUrl);
            var parentNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'forumdisplay')]");

            Interlocked.Increment(ref totalTasks);

            if (parentNodes != null)
            {
                await Parallel.ForEachAsync(parentNodes, async (parentNode, token) =>
                {
                    string hrefValue = parentNode.Attributes["href"].Value;
                    string childPageUrl = Url.CorrectUrl(baseUrl, hrefValue);

                    await ScrapeChildPages(childPageUrl);

                    Interlocked.Increment(ref completedTasks);
                    progressBar.Report((double)completedTasks / totalTasks);
                });
            }

            var parentPaginationNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/page') and contains(@href, 'forumdisplay')]");

            if (parentPaginationNodes != null)
            {
                foreach (var paginationNode in parentPaginationNodes)
                {
                    string hrefValue = paginationNode.Attributes["href"].Value;
                    await ScrapeForumPages(hrefValue);
                }
            }
        }

        private async Task ScrapeChildPages(string url)
        {
            if (visitedUrls.ContainsKey(url))
                return;

            visitedUrls.TryAdd(url, 0);

            string docUrl = Url.CorrectUrl(baseUrl, url);
            HtmlDocument doc = await Html.GetHtmlDocument(docUrl);
            var childNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'showthread')]");

            if (childNodes != null)
            {
                await Parallel.ForEachAsync(childNodes, async (childNode, token) =>
                {
                    string childHrefValue = childNode.Attributes["href"].Value;
                    string childPageUrl = Url.CorrectUrl(baseUrl, childHrefValue);

                    var childDoc = await Html.GetHtmlDocument(childPageUrl);
                    var posts = childDoc.DocumentNode.SelectNodes("//li[contains(@id, 'post_')]");

                    foreach (var post in posts)
                    {
                        string? title = post.SelectSingleNode("//h2")?.InnerText.Trim();
                        string? body = post.SelectSingleNode("//div[contains(@class, 'content')]")?.InnerText.Trim();
                        string? postId = post.GetAttributeValue("id", "1").Replace("post_", string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body)) continue;
                        title = Text.Normalize(title);
                        body = Text.Normalize(body);
                        Contents.TryAdd(new GraphNode(title, body, childPageUrl, postId), 0);
                    }

                    var childPaginationNodes = childDoc.DocumentNode.SelectNodes("//a[contains(@href, '/page') and contains(@href, 'showthread')]");
                    if (childPaginationNodes != null)
                    {
                        foreach (var paginationNode in childPaginationNodes)
                        {
                            string hrefValue = paginationNode.Attributes["href"].Value;
                            string nextChildPageUrl = Url.CorrectUrl(baseUrl, hrefValue);
                            await ScrapeChildPages(nextChildPageUrl);
                        }
                    }
                });
            }
        }
    }
}