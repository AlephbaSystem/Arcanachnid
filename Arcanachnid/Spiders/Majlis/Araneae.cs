using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Arcanachnid.Database.Drivers;
using Arcanachnid.Models;
using Arcanachnid.Utilities;
using Neo4j.Driver;

namespace Arcanachnid.Spiders.Majlis
{
    public class Araneae
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, byte> visitedUrls = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private int batchCount;
        private int completedTasks;
        private MySqlDriver mysqlService;

        public Araneae(string BaseUrl = "https://rc.majlis.ir/fa/law/search?page=1", int BatchCount = 50)
        {
            baseUrl = BaseUrl;
            batchCount = BatchCount;
            progressBar = new ProgressBar();
            mysqlService = new MySqlDriver("Server=localhost;Database=arcanachnid;Uid=sa;Pwd=!!5O1O95O!!");
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

            if (Contents.Where(x => x.Value == 0).Count() > batchCount)
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

            var driver = LoadPageAndWait(docUrl);
            var parentNodes = driver.FindElements(By.XPath("//a[@href]")).ToList();
            parentNodes = parentNodes.Where(x => !visitedUrls.ContainsKey(x.GetAttribute("href")) && (x.GetAttribute("href").Contains("/fa/law/show/") || x.GetAttribute("href").Contains("/fa/law/search/"))).ToList();
            int newTasksCount = parentNodes?.Count ?? 0;
            Interlocked.Add(ref totalTasks, newTasksCount);
            if (parentNodes != null)
            {
                await Parallel.ForEachAsync(parentNodes, async (parentNode, token) =>
                {
                    string hrefValue = parentNode.GetAttribute("href");
                    string childPageUrl = Url.CorrectUrl(baseUrl, hrefValue);
                    if (Url.InSameDomain(baseUrl, childPageUrl))
                    {
                        try
                        {
                            if (hrefValue.Contains("/fa/law/show/"))
                            {
                                await ScrapeArticle(childPageUrl);
                            }
                            else if (hrefValue.Contains("/fa/law/search/"))
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

            var driver = LoadPageAndWait(docUrl);
            ExtractArticleContent(driver, docUrl);
        }

        private ChromeDriver LoadPageAndWait(string url)
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            var driver = new ChromeDriver(options);

            driver.Navigate().GoToUrl(url);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(d => !d.FindElements(By.XPath("//h2[contains(text(), 'در ﺣﺎل اﻧﺘﻘﺎل ﺑﻪ ﺳﺎﯾﺖ ﻣﻮرد ﻧﻈﺮ ﻫﺴﺘﯿﺪ')]")).Any());

            return driver;
        }

        private void ExtractArticleContent(ChromeDriver driver, string docUrl)
        {
            var titleElement = driver.FindElement(By.XPath("/html/body/main/main/section[2]/div/div/div[1]/div/h1"));
            var idateElement = driver.FindElement(By.XPath("/html/body/main/main/section[2]/div/div/div[4]/div[1]/div/div[1]/span[2]"));
            var bodyElement = driver.FindElement(By.XPath("/html/body/main/main/section[2]/div/div/div[7]"));
            var referenceElement = driver.FindElement(By.XPath("/html/body/main/main/section[2]/div/div/div[4]/div[1]/div/div[2]/span[2]/a"));

            var title = titleElement?.Text;
            var idate = idateElement?.Text;
            var date = PersianDateConverter.ConvertPersianToDateTimeYMD(idate);
            var body = bodyElement?.Text;
            var reference = referenceElement;
            var canonical = docUrl;

            var rlist = new List<(string, string)>();
            rlist.Add((reference.Text.Trim(), reference.GetAttribute("href").Trim()));

            if (title != null && body != null)
                Contents.TryAdd(new GraphNode(title, body, null, docUrl, canonical.Split("/").Last(), date, rlist, null), 0);
            else
                _ = 1;
        }
    }
}
