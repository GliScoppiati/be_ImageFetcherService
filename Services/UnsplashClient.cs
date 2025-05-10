using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageFetcherService.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace ImageFetcherService.Services
{
    public class UnsplashClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;

        public UnsplashClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _accessKey = configuration["Unsplash:AccessKey"]
                ?? throw new InvalidOperationException("Unsplash API access key is missing.");
        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 2)
        {
            var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page={perPage}&client_id={_accessKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Unsplash API error ({response.StatusCode}): {errorContent}");
                throw new HttpRequestException($"Unsplash API error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var results = new List<ImageResultDto>();

            foreach (var photo in document.RootElement.GetProperty("results").EnumerateArray())
            {
                results.Add(new ImageResultDto
                {
                    Url = photo.GetProperty("urls").GetProperty("regular").GetString() ?? "",
                    Source = "unsplash",
                    Photographer = photo.GetProperty("user").GetProperty("name").GetString() ?? "",
                    PhotographerUrl = photo.GetProperty("user").GetProperty("links").GetProperty("html").GetString() ?? "",
                    Description = photo.GetProperty("alt_description").GetString() ?? ""
                });
            }

            return results;
        }
    }
}
