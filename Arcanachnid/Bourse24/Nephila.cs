using Arcanachnid.Database.Drivers;
using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

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
        private Neo4jDriver neo4jService;
        private bool batchMode = false;

        public Nephila(string BaseUrl = "https://www.bourse24.ir/articles", bool batchMode = false)
        {
            this.baseUrl = BaseUrl;
            this.progressBar = new ProgressBar();
            this.neo4jService = new Neo4jDriver("bolt://localhost:7687", "neo4j", "neo4j");
            this.batchMode = batchMode;
        }

        public bool IsSaveData()
        {
            return Contents.Any(x => x.Value == 0);
        }

        public async Task<bool> SaveDatabase()
        {
            foreach (var item in Contents.Keys)
            {
                if (Contents[item] == 0)
                {
                    await neo4jService.AddOrUpdateModelAsync(item);
                }
                Contents[item] = 1;
            }
            return Contents.Any(x => x.Value == 0);
        }

        public async Task StartScraping(string startUrl = "")
        {
            progressBar = new ProgressBar();
            await ScrapeArticles(startUrl);
            progressBar.Dispose();
        }

        private async Task ScrapeArticles(string url)
        {
            if (visitedUrls.ContainsKey(url))
                return;

            visitedUrls.TryAdd(url, 0);

            if (Contents.Where(x => x.Value == 0).Count() > 100 && batchMode)
            {
                try
                {
                    await SaveDatabase();
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
            }

            string docUrl = Url.CorrectUrl(baseUrl, url);
            HtmlDocument doc = await Html.GetHtmlDocument(docUrl);
            var parentNodes = doc.DocumentNode.SelectNodes("//article/div/h2/a");

            int newTasksCount = (parentNodes?.Count ?? 0);
            var parentPaginationNodes = doc.DocumentNode.SelectNodes("//html/body/div[1]/div/div[2]/div/div[1]/div[2]/div/ul/li/a");
            if (parentPaginationNodes != null)
            {
                newTasksCount += parentPaginationNodes.Count(node => !visitedUrls.ContainsKey(Url.CorrectUrl(baseUrl, node.Attributes["href"].Value)));
            }
            Interlocked.Add(ref totalTasks, newTasksCount);

            if (parentNodes != null)
            {
                await Parallel.ForEachAsync(parentNodes, async (parentNode, token) =>
                {
                    string hrefValue = parentNode.Attributes["href"].Value;
                    string childPageUrl = Url.CorrectUrl(baseUrl, hrefValue);
                    try
                    {
                        await ScrapeArticle(childPageUrl);
                    }
                    catch (Exception)
                    { 
                    }
                    Interlocked.Increment(ref completedTasks);
                    progressBar.Report((double)completedTasks / totalTasks);
                });
            }

            if (parentPaginationNodes != null)
            {
                foreach (var paginationNode in parentPaginationNodes)
                {
                    string hrefValue = paginationNode.Attributes["href"].Value;
                    if (!visitedUrls.ContainsKey(Url.CorrectUrl(baseUrl, hrefValue)))
                    {
                        await ScrapeArticles(hrefValue);
                    }
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
            if (doc == null)
            {
                return;
            }
            HtmlNodeCollection Nodes = doc.DocumentNode.SelectNodes("//html/body//article//a");
            if (Nodes?.Any(x => x.InnerText.Contains("عضویت و یا ورود به سایت")) == true)
                return;

            var title = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/h1")?.InnerText;
            var idate = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[3]/div/div[1]/div/article/div[1]/div[1]/span[1]")?.InnerText;
            var date = PersianDateConverter.ConvertPersianToDateTime(idate);
            var category = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[3]/div/div[1]/div/article/div[1]/div[1]/span[2]/a")?.InnerText;
            var body = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[3]/div/div[1]/div/article/div[1]/div[2]")?.InnerHtml;
            var reference =  doc.DocumentNode.SelectNodes("//article/p/a");
            var canonical = doc.DocumentNode.SelectSingleNode("/html/head/link[3]")?.GetAttributeValue("href", "");
            var rlist = new List<(string, string)>();
            if (reference != null)
            {
                foreach (var item in reference)
                {
                    rlist.Add((item.InnerText, item.GetAttributeValue("href", "")));
                }
            }
            var tags = doc.DocumentNode.SelectNodes("//article/div[2]/a");
            var tlist = new List<string>();
            if (tags != null)
            {
                foreach (var item in tags)
                {
                    tlist.Add(item.InnerText);
                }
            }
            if (title != null && body != null)
                Contents.TryAdd(new GraphNode(title, body, category, docUrl, canonical.Split("/").Last(), date, rlist, tlist), 0);
        }
    }
}