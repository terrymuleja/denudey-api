namespace Denudey.Api.Models.DTOs
{
    public class ScamFlixEpisodeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
