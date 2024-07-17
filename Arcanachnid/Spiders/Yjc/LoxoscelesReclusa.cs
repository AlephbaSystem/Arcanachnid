using Arcanachnid.Database.Drivers;
using Arcanachnid.Models;
using Arcanachnid.Utilities;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Spiders.Yjc
{
    public class LoxoscelesReclusa
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int completedTasks;
        private MySqlDriver mysqlService;
        private bool batchMode = false;

        public LoxoscelesReclusa(string BaseUrl = "https://www.yjc.ir/fa/list/6/44?sid=6&catid=44&page=1", bool batchMode = false)
        {
            baseUrl = BaseUrl;
            progressBar = new ProgressBar();
            mysqlService = new MySqlDriver("Server=localhost;Database=arcanachnid;Uid=sa;Pwd=!!5O1O95O!!");
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
                    await mysqlService.AddOrUpdateModelAsync(item);
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

            if (Contents.Where(x => x.Value == 0).Count() > 50 && batchMode)
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
            if (string.IsNullOrEmpty(docUrl))
            {
                return;
            }
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
                            if (hrefValue.Contains("/fa/news/"))
                            {
                                await ScrapeArticle(childPageUrl);
                            }
                            else
                            {
                                await ScrapeArticles(childPageUrl);
                            }
                        }
                        catch (Exception ex)
                        {
                            _ = ex;
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

            var title = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'title-news')]")?.InnerText;
            var idate = doc.DocumentNode.SelectSingleNode("/html/body/div[5]/div/div/div/div[1]/div/div/div/div[1]/div/div/div[2]/div[1]/div[1]/span/span")?.InnerText;
            var date = PersianDateConverter.ConvertPersianToDateTimeTDYM(idate);
            var body = doc.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' row ') and contains(concat(' ', normalize-space(@class), ' '), ' baznashr-body ')]")?.InnerHtml;
            var reference = doc.DocumentNode.SelectNodes("//html/body/div[5]/div/div/div/div[1]/div/div/div/div[4]/div/div[1]/div/a");
            var canonical = doc.DocumentNode.SelectSingleNode("/html/head/link[@rel='amphtml']")?.GetAttributeValue("href", "");

            var rlist = new List<(string, string)>();
            if (reference != null)
            {
                foreach (var item in reference)
                {
                    rlist.Add((item.InnerText.Trim(), item.GetAttributeValue("href", "").Trim()));
                }
            }
            var tags = doc.DocumentNode.SelectNodes("//div[contains(@class, 'path_bottom_body')]//a");
            var tlist = new List<string>();
            if (tags != null)
            {
                bool skip = true;
                foreach (var item in tags)
                {
                    if (skip)
                    {
                        skip = !skip;
                        continue;
                    }
                    tlist.Add(item.InnerText.Trim());
                }
            }
            var category = tlist.FirstOrDefault();
            if (title != null && body != null)
                Contents.TryAdd(new GraphNode(title, body, category, docUrl, canonical.Split("/").Last(), date, rlist, tlist), 0);
            else
                _ = 1;
        }
    }
}