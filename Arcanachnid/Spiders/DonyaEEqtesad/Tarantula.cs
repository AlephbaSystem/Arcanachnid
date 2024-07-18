using Arcanachnid.Database.Drivers;
using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Spiders.DonyaEEqtesad
{
    public class Tarantula
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int completedTasks;
        private Neo4jDriver neo4jService;
        private MySqlDriver mysqlService;
        private bool batchMode = false;
        private bool _mysql = false;

        public Tarantula(string BaseUrl = "https://donya-e-eqtesad.com/", bool batchMode = false, bool mysql = false)
        {
            baseUrl = BaseUrl;
            progressBar = new ProgressBar();
            neo4jService = new Neo4jDriver("bolt://localhost:7687", "neo4j", "neo4j");
            mysqlService = new MySqlDriver("Server=localhost;Database=arcanachnid;Uid=sa;Pwd=!!5O1O95O!!;");
            this.batchMode = batchMode;
            _mysql = mysql;
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
                    if (_mysql)
                    {
                        await mysqlService.AddOrUpdateModelAsync(item);
                    }
                    else
                    {
                        await neo4jService.AddOrUpdateModelAsync(item);
                    }
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
            var parentNodes = doc.DocumentNode.SelectNodes("//a[@href]").ToList();
            parentNodes = parentNodes.Where(x => !visitedUrls.ContainsKey(x.Attributes["href"].Value)).ToList();
            int newTasksCount = parentNodes?.Count ?? 0;
            Interlocked.Add(ref totalTasks, newTasksCount);
            if (parentNodes != null)
            {
                await Parallel.ForEachAsync(parentNodes, async (parentNode, token) =>
                {
                    string hrefValue = parentNode.Attributes["href"].Value;
                    string childPageUrl = Url.CorrectUrl(baseUrl, hrefValue);
                    if (Url.InSameDomain(baseUrl, childPageUrl))
                    {
                        try
                        {
                            if (hrefValue.Contains("/news/"))
                            {
                                await ScrapeArticle(childPageUrl);
                            }
                            else
                            {
                                await ScrapeArticles(childPageUrl);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Interlocked.Increment(ref completedTasks);
                    progressBar.Report((double)completedTasks / totalTasks);
                });
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

            var title = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'title')]")?.InnerText;
            var idate = doc.DocumentNode.SelectSingleNode("//time[contains(@class, 'news-time')]")?.InnerText;
            var date = PersianDateConverter.ConvertPersianToDateTime(idate);
            var body = doc.DocumentNode.SelectSingleNode("//div[@id='echo-detail']")?.InnerHtml;
            var reference = doc.DocumentNode.SelectNodes("//html/body/div[2]/div[2]/div[3]/div/div[1]/div[7]/div[2]/div/article/h3/a");
            var canonical = doc.DocumentNode.SelectSingleNode("/html/head/link[2]")?.GetAttributeValue("content", "");
            var rlist = new List<(string, string)>();
            if (reference != null)
            {
                foreach (var item in reference)
                {
                    rlist.Add((item.InnerText, item.GetAttributeValue("href", "")));
                }
            }
            var tags = doc.DocumentNode.SelectNodes("//html/body/div[1]/div[2]/main/div[1]/div[3]/div[1]/div[1]/article/div[3]/div/a");
            var tlist = new List<string>();
            if (tags != null)
            {
                foreach (var item in tags)
                {
                    tlist.Add(item.InnerText);
                }
            }
            var category = tlist.FirstOrDefault();
            if (title != null && body != null)
                Contents.TryAdd(new GraphNode(title, body, category, docUrl, canonical.Split("/").Last(), date, rlist, tlist), 0);
        }
    }
}
