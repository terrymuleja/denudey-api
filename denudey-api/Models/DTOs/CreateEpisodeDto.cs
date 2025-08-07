namespace Denudey.Api.Models.DTOs
{
    public class CreateEpisodeDto
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string ImageUrl { get; set; } = string.Empty;
    }

}
