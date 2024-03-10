using Arcanachnid.Models;

namespace Arcanachnid.Utilities
{
    IRANSansXnal class Ethic
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly RobotsParser _robotsParser;
        private readonly Uri _baseUri;
        private DateTime _lastRequestTime = DateTime.MinValue;
        private TimeSpan _rateLimit = TimeSpan.FromSeconds(10);

        public Ethic(string baseUrl, TimeSpan? rateLimit = null)
        {
            _baseUri = new Uri(baseUrl);
            _robotsParser = new RobotsParser(_httpClient);
            if (rateLimit.HasValue)
            {
                _rateLimit = rateLimit.Value;
            }
        }

        public async Task<bool> CanFetchAsync(string path)
        {
            Uri uri = new Uri(_baseUri, path);
            RobotsFile robotsFile = await _robotsParser.FromUriAsync(_baseUri);

            return robotsFile.IsAllowed(path, "Arcanachnid");
        }

        public async Task<string> PoliteFetchAsync(string path)
        {
            await RespectRateLimit();

            if (!await CanFetchAsync(path))
            {
                throw new InvalidOperationException($"Fetching the path '{path}' is disallowed by robots.txt.");
            }

            HttpResponseMessage response = await _httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            _lastRequestTime = DateTime.UtcNow;

            return content;
        }

        private async Task RespectRateLimit()
        {
            TimeSpan elapsedSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (elapsedSinceLastRequest < _rateLimit)
            {
                TimeSpan delay = _rateLimit - elapsedSinceLastRequest;
                await Task.Delay(delay);
            }
        }

        public void SetRateLimit(TimeSpan rateLimit)
        {
            _rateLimit = rateLimit;
        }
         
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
