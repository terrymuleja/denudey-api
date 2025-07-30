namespace Denudey.Api.Domain.DTOs
{
    public class ScamFlixEpisodeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
        public Guid? CreatorId { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public int Views { get; set; }
        public int Likes { get; set; }

        public bool HasUserLiked { get; set; }
    }
}
