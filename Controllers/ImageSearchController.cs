using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public ImageSearchController(PexelsClient pexelsClient, PixabayClient pixabayClient, UnsplashClient unsplashClient)
        {
            _pexelsClient = pexelsClient;
            _pixabayClient = pixabayClient;
            _unsplashClient = unsplashClient;
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new
                {
                    message = "Query is required",
                    timestamp = DateTime.UtcNow
                });

            var imageResults = new List<ImageResultDto>();
            var tasks = new List<Task>();
            int fallbackCount = 0;

            // UNSPLASH (4 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var unsplash = await _unsplashClient.SearchImagesAsync(query, 4);
                    imageResults.AddRange(unsplash);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403"))
                {
                    Console.WriteLine("⚠️ Unsplash rate limit reached.");
                    fallbackCount = 2;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Unsplash error: {ex.Message}");
                }
            }));

            // PEXELS (fallisce il caricamento, fallback)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var pexels = await _pexelsClient.SearchImagesAsync(query, 5 + fallbackCount / 2);
                    imageResults.AddRange(pexels);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Pexels error: {ex.Message}");
                    fallbackCount += 10;
                }
            }));

            // PIXABAY (11 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var pixabay = await _pixabayClient.SearchImagesAsync(query, 11);
                    imageResults.AddRange(pixabay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Pixabay error: {ex.Message}");
                }
            }));

            // Attendere che tutte le richieste siano completate
            await Task.WhenAll(tasks);

            if (!imageResults.Any())
                return NoContent();

            // Randomizzare i risultati prima di restituirli
            var randomizedResults = imageResults.OrderBy(x => Guid.NewGuid()).ToList();

            return Ok(randomizedResults);
        }
    }
}
