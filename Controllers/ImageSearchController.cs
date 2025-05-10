using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImageFetcherService.Services;
using ImageFetcherService.Models;

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
                return BadRequest("Query is required");

            var imageResults = new List<ImageResultDto>();
            var tasks = new List<Task>();
            int fallbackCount = 0;
            const int unsplashCount = 2;

            // UNSPLASH (2 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var unsplash = await _unsplashClient.SearchImagesAsync(query, unsplashCount);
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

            // PEXELS (5 o 6 immagini)
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
                }
            }));

            // PIXABAY (5 o 6 immagini)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var pixabay = await _pixabayClient.SearchImagesAsync(query, 5 + (fallbackCount + 1) / 2);
                    imageResults.AddRange(pixabay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Pixabay error: {ex.Message}");
                }
            }));

            await Task.WhenAll(tasks);

            if (!imageResults.Any())
                return NoContent();

            return Ok(imageResults.OrderBy(r => r.Source).ToList());
        }
    }
}
