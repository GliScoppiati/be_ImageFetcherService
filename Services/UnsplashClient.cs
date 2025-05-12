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
    public class UnsplashClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;
        private readonly ILogger<UnsplashClient> _logger;

        public UnsplashClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<UnsplashClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _accessKey = configuration["Unsplash:AccessKey"]
                          ?? throw new InvalidOperationException("Unsplash API access key is missing from configuration.");

            _logger.LogDebug("[ImageFetcherService] 🔐 Unsplash access key loaded successfully.");
        }

        public async Task<List<ImageResultDto>> SearchImagesAsync(string query, int perPage = 2)
        {
            var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page={perPage}&client_id={_accessKey}";
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);
                _logger.LogInformation(
                    "[ImageFetcherService] 🌐 Sent Unsplash request for '{Query}' (perPage={PerPage}).",
                    query,
                    perPage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[ImageFetcherService] ❌ Failed HTTP request to Unsplash for '{Query}'.",
                    query
                );
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[ImageFetcherService] ❌ Unsplash API error for '{Query}' (StatusCode: {StatusCode}). Response: {ErrorContent}",
                    query,
                    response.StatusCode,
                    errorContent
                );
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

            _logger.LogInformation(
                "[ImageFetcherService] 🚀 Unsplash returned {Count} images for '{Query}'.",
                results.Count,
                query
            );

            return results;
        }
    }
}
