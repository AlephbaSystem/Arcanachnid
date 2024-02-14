using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Arcanachnid.Bourse24
{
    public class Nephila
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int completedTasks;

        public Nephila(string BaseUrl = "https://www.bourse24.ir/articles")
        {
            baseUrl = BaseUrl;
            progressBar = new ProgressBar();
        }

        public async Task<List<KeyValuePair<GraphNode, byte>>> StartScraping(string startUrl = "")
        {
            progressBar = new ProgressBar();
            await ScrapeArticles(startUrl);
            progressBar.Dispose();
            return Contents.ToList();
        }

        private async Task ScrapeArticles(string url)
        {
            if (visitedUrls.ContainsKey(url))
                return;

            visitedUrls.TryAdd(url, 0);

            string docUrl = Url.CorrectUrl(baseUrl, url);
            HtmlDocument doc = await Html.GetHtmlDocument(docUrl);
            var parentNodes = doc.DocumentNode.SelectNodes("//article/div/h2/a");

            Interlocked.Increment(ref totalTasks);

            if (parentNodes != null)
            {
                await Parallel.ForEachAsync(parentNodes, async (parentNode, token) =>
                {
                    string hrefValue = parentNode.Attributes["href"].Value;
                    string childPageUrl = Url.CorrectUrl(baseUrl, hrefValue);

                    await ScrapeArticle(childPageUrl);

                    Interlocked.Increment(ref completedTasks);
                    progressBar.Report((double)completedTasks / totalTasks);
                });
            }

            var parentPaginationNodes = doc.DocumentNode.SelectNodes("//html/body/div[1]/div/div[2]/div/div[1]/div[2]/div/ul/li/a");

            if (parentPaginationNodes != null)
            {
                foreach (var paginationNode in parentPaginationNodes)
                {
                    string hrefValue = paginationNode.Attributes["href"].Value;
                    await ScrapeArticles(hrefValue);
                }
            }
        }

        private async Task ScrapeArticle(string url)
        {
            if (visitedUrls.ContainsKey(url))
                return;

            visitedUrls.TryAdd(url, 0);

            string docUrl = Url.CorrectUrl(baseUrl, url);
            HtmlDocument doc = await Html.GetHtmlDocument(docUrl);

            var isPrivate  = doc.DocumentNode.SelectSingleNode("/article/div/section/div[2]/div/a")?.InnerText?.Contains("عضویت و یا ورود به سایت");
            if (isPrivate == true)
                return;

            var title = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/h1").InnerText;
            var date = doc.DocumentNode.SelectSingleNode("/article/div[1]/div[1]/span[1]").InnerText;
            var category = doc.DocumentNode.SelectSingleNode("/article/div[1]/div[1]/span[2]/a").InnerText;
            var body = doc.DocumentNode.SelectSingleNode("/article/div[1]").InnerHtml;
            var reference =  doc.DocumentNode.SelectNodes("//article/p/a");
            var canonical = doc.DocumentNode.SelectSingleNode("/html/head/link[3]").GetAttributeValue("href", "");
            var rlist = new List<(string, string)>();
            foreach (var item in reference)
            {
                rlist.Add((item.InnerText, item.GetAttributeValue("href", "")));
            }
            var tags = doc.DocumentNode.SelectNodes("//article/div[2]/a");
            var tlist = new List<string>();
            foreach (var item in tags)
            {
                tlist.Add(item.InnerText);
            }
            Contents.TryAdd(new GraphNode(title, body, category, docUrl, canonical.Split("/").Last(), date, rlist, tlist), 0);
        }
    }
}