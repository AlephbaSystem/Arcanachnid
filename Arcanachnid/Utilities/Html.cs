using HtmlAgilityPack;

namespace Arcanachnid.Utilities
{
    public static class Html
    {
        public static async Task<HtmlDocument> GetHtmlDocument(string url)
        {
            Bruennichi client = await Bruennichi.CreateAsync(url, TimeSpan.FromSeconds(10));
            var response = await client.GetAsync(url);
            var pageContents = await response.Content.ReadAsStringAsync();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContents);

            return doc;
        }
        public static async Task<string> GetJsonString(string url)
        {
            Bruennichi client = await Bruennichi.CreateAsync(url, TimeSpan.FromSeconds(10));
            var response = await client.GetAsync(url);
            if (response.Content.Headers.ContentType.MediaType == "application/json")
            {
                return await response.Content.ReadAsStringAsync();
            }
            throw new HttpRequestException("Expected JSON response, but content type was not application/json.");
        }
    }
}
