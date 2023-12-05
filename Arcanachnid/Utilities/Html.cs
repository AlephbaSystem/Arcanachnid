using HtmlAgilityPack;

namespace Arcanachnid.Utilities
{
    public static class Html
    {
        public static async Task<HtmlDocument> GetHtmlDocument(string url)
        {
            Argiope client = await Argiope.CreateAsync(url, TimeSpan.FromSeconds(10)); 
            var response = await client.GetAsync(url);
            var pageContents = await response.Content.ReadAsStringAsync();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContents);

            return doc;
        }
    }
}
