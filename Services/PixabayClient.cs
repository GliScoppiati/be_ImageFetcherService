using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageFetcherService.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace ImageFetcherService.Services
{
    public class PixabayClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public PixabayClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Pixabay:ApiKey"] ?? throw new InvalidOperationException("Pixabay API key is missing.");
        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 5)
        {
            var url = $"https://pixabay.com/api/?key={_apiKey}&q={Uri.EscapeDataString(query)}&per_page={perPage}&image_type=photo";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Pixabay API error ({response.StatusCode}): {errorContent}");
                throw new HttpRequestException($"Pixabay API error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);

            var results = new List<ImageResultDto>();

            foreach (var hit in document.RootElement.GetProperty("hits").EnumerateArray())
            {
                results.Add(new ImageResultDto
                {
                    Url = hit.GetProperty("webformatURL").GetString() ?? "",
                    Source = "pixabay",
                    Photographer = hit.GetProperty("user").GetString() ?? "",
                    PhotographerUrl = $"https://pixabay.com/users/{hit.GetProperty("user").GetString()?.ToLower()}/",
                    Description = hit.GetProperty("tags").GetString() ?? ""
                });
            }

            return results;
        }
    }
}
