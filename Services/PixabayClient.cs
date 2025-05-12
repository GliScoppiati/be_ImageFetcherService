using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageFetcherService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace ImageFetcherService.Services
{
    public class PixabayClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<PixabayClient> _logger;

        public PixabayClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PixabayClient> logger)
        {
            _httpClient = httpClient;
            _logger     = logger;
            _apiKey      = configuration["Pixabay:ApiKey"] 
                          ?? throw new InvalidOperationException("Pixabay API key is missing from configuration.");
            _logger.LogDebug("[ImageFetcherService] 🔐 Pixabay API key loaded successfully.");
        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 5)
        {
            var url = $"https://pixabay.com/api/?key={_apiKey}&q={Uri.EscapeDataString(query)}&per_page={perPage}&image_type=photo";
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[ImageFetcherService] ❌ Pixabay HTTP request failed for '{Query}'.",
                    query
                );
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[ImageFetcherService] ❌ Pixabay API error for '{Query}' (StatusCode: {StatusCode}). Response: {ErrorContent}",
                    query,
                    response.StatusCode,
                    errorContent
                );
                throw new HttpRequestException($"Pixabay API error: {response.StatusCode}");
            }

            var json     = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var results  = new List<ImageResultDto>();

            foreach (var hit in document.RootElement.GetProperty("hits").EnumerateArray())
            {
                results.Add(new ImageResultDto
                {
                    Url             = hit.GetProperty("webformatURL").GetString() ?? "",
                    Source          = "pixabay",
                    Photographer    = hit.GetProperty("user").GetString() ?? "",
                    PhotographerUrl = $"https://pixabay.com/users/{hit.GetProperty("user").GetString()?.ToLower()}/",
                    Description     = hit.GetProperty("tags").GetString() ?? ""
                });
            }

            _logger.LogInformation(
                "[ImageFetcherService] 🚀 Pixabay returned {Count} images for '{Query}'.",
                results.Count,
                query
            );

            return results;
        }
    }
}
