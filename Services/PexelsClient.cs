using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageFetcherService.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace ImageFetcherService.Services
{
    public class PexelsClient
    {
        private readonly HttpClient _httpClient;

        public PexelsClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var apiKey = configuration["Pexels:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("❌ Pexels API key is missing from configuration.");
            }

            Console.WriteLine($"🔐 Pexels API key loaded: {apiKey.Substring(0, 4)}********");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ImageFetcherBot/1.0)");

        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 5)
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={perPage}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Pexels API call failed ({response.StatusCode}): {errorContent}");
                throw new HttpRequestException($"Pexels API error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var results = new List<ImageResultDto>();

            foreach (var photo in document.RootElement.GetProperty("photos").EnumerateArray())
            {
                results.Add(new ImageResultDto
                {
                    Url = photo.GetProperty("src").GetProperty("medium").GetString() ?? "",
                    Source = "pexels",
                    Photographer = photo.GetProperty("photographer").GetString() ?? "",
                    PhotographerUrl = photo.GetProperty("photographer_url").GetString() ?? "",
                    Description = photo.GetProperty("alt").GetString() ?? ""
                });
            }

            return results;
        }
    }
}
