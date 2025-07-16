namespace Denudey.Api.Models.DTOs
{
    public class CreateEpisodeDto
    {
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        public string ImageUrl { get; set; }
    }

}
