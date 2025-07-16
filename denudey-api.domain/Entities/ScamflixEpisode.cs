namespace Denudey.Api.Domain.Entities
{
    public class ScamflixEpisode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; } // comma-separated
        public string ImageUrl { get; set; }
        public string CreatedBy { get; set; } // optional: user ID or username
        public DateTime CreatedAt { get; set; }
    }
}
