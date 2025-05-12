using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageFetcherService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace ImageFetcherService.Services
{
    public class PexelsClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PexelsClient> _logger;

        public PexelsClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PexelsClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var apiKey = configuration["Pexels:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "[ImageFetcherService] ❌ Pexels API key is missing from configuration."
                );
                throw new InvalidOperationException("Pexels API key is missing from configuration.");
            }

            _logger.LogDebug("[ImageFetcherService] 🔐 Pexels API key loaded successfully.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.UserAgent
                .ParseAdd("Mozilla/5.0 (compatible; ImageFetcherBot/1.0)");
        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 5)
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={perPage}";
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[ImageFetcherService] ❌ Pexels HTTP request failed for '{Query}'.",
                    query
                );
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[ImageFetcherService] ❌ Pexels API call failed for '{Query}' (StatusCode: {StatusCode}). Response: {ErrorContent}",
                    query,
                    response.StatusCode,
                    errorContent
                );
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

            _logger.LogInformation(
                "[ImageFetcherService] 🚀 Pexels returned {Count} images for '{Query}'.",
                results.Count,
                query
            );

            return results;
        }
    }
}
