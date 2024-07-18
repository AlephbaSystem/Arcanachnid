using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Arcanachnid.Database.Drivers;
using Arcanachnid.Models;
using Arcanachnid.Utilities;

namespace Arcanachnid.Spiders.Majlis
{
    public class Araneae : IDisposable
    {
        private ProgressBar progressBar;
        private ConcurrentDictionary<string, int> visitedUrls = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<GraphNode, byte> Contents = new ConcurrentDictionary<GraphNode, byte>();
        private readonly string baseUrl;
        private int totalTasks;
        private readonly int batchCount;
        private int completedTasks;
        private MySqlDriver mysqlService;
        private ChromeDriver driver;
        private readonly int retry;

        public Araneae(string BaseUrl = "https://rc.majlis.ir/fa/law/search?page=1", int BatchCount = 50, int Retry = 5)
        {
            baseUrl = BaseUrl;
            batchCount = BatchCount;
            progressBar = new ProgressBar();
            mysqlService = new MySqlDriver("Server=localhost;Database=arcanachnid;Uid=sa;Pwd=!!5O1O95O!!");
            var options = new ChromeOptions();
            options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            options.AddArgument("--headless");
            options.AddArguments("--disable-logging");
            options.AddArguments("--silent");
            options.AddArguments("--log-level=3");
            driver = new ChromeDriver(options);
            retry = Retry;
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
            visitedUrls.TryAdd(baseUrl, retry);
            while (visitedUrls.Any(x => x.Value != 0))
            {
                var hrefValue = visitedUrls.FirstOrDefault(x => x.Value != 0);
                if (hrefValue.Equals(default(KeyValuePair<string, int>)))
                {
                    break; // No more URLs to process
                }

                if (hrefValue.Key.Contains("/fa/law/show"))
                {
                    await ScrapeArticle(hrefValue.Key);
                }
                else if (hrefValue.Key.Contains("/fa/law/search"))
                {
                    await ScrapeArticles(hrefValue.Key);
                }

                // Ensure the progress bar updates
                progressBar.Report((double)completedTasks / totalTasks);
            }
            progressBar.Dispose();
        }

        private async Task ScrapeArticles(string url)
        {
            if (visitedUrls.ContainsKey(url))
            {
                if (visitedUrls[url] == 0)
                    return;
                else
                    visitedUrls[url] -= 1;
            }
            else
            {
                visitedUrls.TryAdd(url, 0);
            }

            if (Contents.Count(x => x.Value == 0) > batchCount)
            {
                try
                {
                    await SaveDatabase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving database: {ex.Message}");
                }
            }

            string docUrl = Utilities.Url.CorrectUrl(baseUrl, url);
            if (string.IsNullOrEmpty(docUrl))
            {
                return;
            }
            try
            {
                LoadPageAndWait(docUrl);
                var parentNodes = driver.FindElements(By.XPath("//a[@href]")).ToList();
                parentNodes = parentNodes.Where(x => !visitedUrls.ContainsKey(x.GetAttribute("href")) && (x.GetAttribute("href").Contains("/fa/law/show") || x.GetAttribute("href").Contains("/fa/law/search"))).ToList();
                int newTasksCount = parentNodes?.Count ?? 0;
                Interlocked.Add(ref totalTasks, newTasksCount);
                if (parentNodes != null)
                {
                    Parallel.ForEach(parentNodes, parentNode =>
                    {
                        string hrefValue = parentNode.GetAttribute("href");
                        string childPageUrl = Utilities.Url.CorrectUrl(baseUrl, hrefValue);

                        if (Utilities.Url.InSameDomain(baseUrl, childPageUrl))
                        {
                            if (visitedUrls.ContainsKey(childPageUrl)) return;
                            visitedUrls.TryAdd(childPageUrl, retry);
                        }
                        Interlocked.Increment(ref completedTasks);
                        progressBar.Report((double)completedTasks / totalTasks);
                    });
                    visitedUrls[docUrl] = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping articles: {ex.Message}");
            }
        }

        private async Task ScrapeArticle(string url)
        {
            if (visitedUrls.ContainsKey(url))
            {
                if (visitedUrls[url] == 0)
                    return;
                else
                    visitedUrls[url] -= 1;
            }
            else
            {
                visitedUrls.TryAdd(url, 0);
            }

            string docUrl = Utilities.Url.CorrectUrl(baseUrl, url);
            try
            {
                LoadPageAndWait(docUrl);
                ExtractArticleContent(docUrl);
            }
            catch (Exception)
            { 
            }
        }

        private void LoadPageAndWait(string url)
        {
            driver.Navigate().GoToUrl(url);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(d => !d.FindElements(By.XPath("//h2[contains(text(), 'در ﺣﺎل اﻧﺘﻘﺎل ﺑﻪ ﺳﺎﯾﺖ ﻣﻮرد ﻧﻈﺮ ﻫﺴﺘﯿﺪ')]")).Any());
        }

        private void ExtractArticleContent(string docUrl)
        {
            try
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
                {
                    Contents.TryAdd(new GraphNode(title, body, null, docUrl, canonical.Split("/").Last(), date, rlist, null), 0);
                    visitedUrls[docUrl] = 0;
                }
                else
                {
                    Console.WriteLine("Title or Body is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting article content: {ex.Message}");
            }
        }

        public void Dispose()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}
