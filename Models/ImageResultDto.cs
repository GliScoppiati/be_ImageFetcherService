namespace ImageFetcherService.Models
{
    public class ImageResultDto
    {
        public string Url { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Photographer { get; set; } = string.Empty;
        public string PhotographerUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}