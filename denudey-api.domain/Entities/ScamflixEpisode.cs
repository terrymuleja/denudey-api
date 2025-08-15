namespace Denudey.Api.Domain.Entities
{
    public class ScamflixEpisode
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;// comma-separated
        public string ImageUrl { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; } // optional: user ID or username
        public DateTime CreatedAt { get; set; }

        public ApplicationUser Creator { get; set; }

    }
}
