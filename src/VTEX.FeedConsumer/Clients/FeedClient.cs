using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VTEX.FeedConsumer.Clients
{
    internal class FeedClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _appKey;
        private readonly string _appToken;

        public FeedClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["VTEX:Feed:BaseAddress"]);
            _appKey = configuration["VTEX:Feed:AppKey"];
            _appToken = configuration["VTEX:Feed:AppToken"];
        }

        public async Task<IEnumerable<FeedDequeueResponse>> DequeueAsync(CancellationToken cancellationToken)
        {
            var path = "api/orders/feed";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-VTEX-API-AppKey", _appKey);
            request.Headers.Add("X-VTEX-API-AppToken", _appToken);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new Exception($"Unable to execute GET in the path `{path}`, the status code was `{response.StatusCode}`");

            var contentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<IEnumerable<FeedDequeueResponse>>(contentString, jsonSerializerOptions);
        }

        public async Task CommitAsync(IEnumerable<string> handles, CancellationToken cancellationToken)
        {
            var path = "api/orders/feed";
            var contentString = JsonSerializer.Serialize(new { handles });
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(contentString)
            };
            request.Headers.Add("X-VTEX-API-AppKey", _appKey);
            request.Headers.Add("X-VTEX-API-AppToken", _appToken);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new Exception($"Unable to execute POST in the path `{path}`, the status code was `{response.StatusCode}`");
        } 
    }
}
