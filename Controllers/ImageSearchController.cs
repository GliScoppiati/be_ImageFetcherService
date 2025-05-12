using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageFetcherService.Services;
using ImageFetcherService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ImageFetcherService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageSearchController : ControllerBase
    {
        private readonly PexelsClient _pexelsClient;
        private readonly PixabayClient _pixabayClient;
        private readonly UnsplashClient _unsplashClient;
        private readonly ILogger<ImageSearchController> _logger;

        public ImageSearchController(
            PexelsClient pexelsClient,
            PixabayClient pixabayClient,
            UnsplashClient unsplashClient,
            ILogger<ImageSearchController> logger)
        {
            _pexelsClient   = pexelsClient;
            _pixabayClient  = pixabayClient;
            _unsplashClient = unsplashClient;
            _logger         = logger;
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("[ImageFetcherService] ⚠️ Search called without query at {Timestamp}.", DateTime.UtcNow);
                return BadRequest(new
                {
                    message   = "Query is required",
                    timestamp = DateTime.UtcNow
                });
            }

            var imageResults = new List<ImageResultDto>();
            var tasks        = new List<Task>();
            int fallbackCount = 0;

            // UNSPLASH (4 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var unsplash = await _unsplashClient.SearchImagesAsync(query, 4);
                    imageResults.AddRange(unsplash);
                    _logger.LogInformation(
                        "[ImageFetcherService] 🚀 Unsplash returned {Count} images for '{Query}'.",
                        unsplash.Count,
                        query
                    );
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403"))
                {
                    fallbackCount = 2;
                    _logger.LogWarning(
                        ex,
                        "[ImageFetcherService] ⚠️ Unsplash rate limit reached for '{Query}'. Using fallback count = {FallbackCount}.",
                        query,
                        fallbackCount
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ImageFetcherService] ❌ Unexpected Unsplash error for '{Query}': {ErrorMessage}",
                        query,
                        ex.Message
                    );
                }
            }));

            // PEXELS (fallisce il caricamento, fallback)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var count = 5 + fallbackCount / 2;
                    var pexels = await _pexelsClient.SearchImagesAsync(query, count);
                    imageResults.AddRange(pexels);
                    _logger.LogInformation(
                        "[ImageFetcherService] 🚀 Pexels returned {Count} images for '{Query}' (requested {Requested}).",
                        pexels.Count,
                        query,
                        count
                    );
                }
                catch (Exception ex)
                {
                    fallbackCount += 10;
                    _logger.LogWarning(
                        ex,
                        "[ImageFetcherService] ⚠️ Pexels error for '{Query}': {ErrorMessage}. Increasing fallbackCount to {FallbackCount}.",
                        query,
                        ex.Message,
                        fallbackCount
                    );
                }
            }));

            // PIXABAY (11 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var pixabay = await _pixabayClient.SearchImagesAsync(query, 11);
                    imageResults.AddRange(pixabay);
                    _logger.LogInformation(
                        "[ImageFetcherService] 🚀 Pixabay returned {Count} images for '{Query}'.",
                        pixabay.Count,
                        query
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ImageFetcherService] ❌ Pixabay error for '{Query}': {ErrorMessage}",
                        query,
                        ex.Message
                    );
                }
            }));

            await Task.WhenAll(tasks);

            if (!imageResults.Any())
            {
                _logger.LogInformation(
                    "[ImageFetcherService] 🔍 No images found for '{Query}', returning NoContent.",
                    query
                );
                return NoContent();
            }

            var randomizedResults = imageResults.OrderBy(x => Guid.NewGuid()).ToList();
            _logger.LogInformation(
                "[ImageFetcherService] 🎲 Returning {Count} randomized images for '{Query}'.",
                randomizedResults.Count,
                query
            );

            return Ok(randomizedResults);
        }
    }
}
