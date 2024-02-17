using Arcanachnid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Utilities
{
    internal class Bruennichi : HttpClient
    {
        private Uri _baseUri;
        private RobotsFile _robotsFile;
        private TimeSpan _rateLimit;
        private DateTime _lastRequestTime;
        private const string DefaultUserAgent = "Arcanachnid";

        public Bruennichi(string baseUrl, TimeSpan rateLimit) : base(new HttpClientHandler(), true)
        {
            _baseUri = new Uri(baseUrl);
            _rateLimit = rateLimit;
            _lastRequestTime = DateTime.MinValue;

            DefaultRequestHeaders.UserAgent.ParseAdd(DefaultUserAgent);

            InitializeRobotsFile(baseUrl).Wait();
        }

        private async Task InitializeRobotsFile(string baseUrl)
        {
            RobotsParser robotsParser = new RobotsParser(new HttpClient());
            _robotsFile = await robotsParser.FromUriAsync(new Uri(baseUrl));
        }

        private async Task<bool> IsRequestAllowedAsync(HttpRequestMessage request)
        {
            string path = request.RequestUri.PathAndQuery;
            string userAgent = "Arcanachnid";

            return _robotsFile.IsAllowed(path, userAgent);
        }

        public static async Task<Bruennichi> CreateAsync(string baseUrl, TimeSpan rateLimit)
        {
            var client = new Bruennichi(baseUrl, rateLimit);
            await client.InitializeRobotsFile(baseUrl);
            return client;
        }

        private async Task RespectRateLimit()
        {
            TimeSpan elapsedSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (elapsedSinceLastRequest < _rateLimit)
            {
                TimeSpan delayDuration = _rateLimit - elapsedSinceLastRequest;
                await Task.Delay(delayDuration);
            }
        }
        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.UserAgent.Count == 0)
            {
                request.Headers.UserAgent.ParseAdd(DefaultUserAgent);
            }

            if (!await IsRequestAllowedAsync(request))
            {
                throw new HttpRequestException("Request is disallowed by robots.txt");
            }

            await RespectRateLimit();
            _lastRequestTime = DateTime.UtcNow;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}