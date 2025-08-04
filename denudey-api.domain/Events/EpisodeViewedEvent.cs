

using System.ComponentModel.DataAnnotations;

namespace Denudey.Api.Domain.Events
{
    public class EpisodeViewedEvent : DomainEvent
    {
        public int EpisodeId { get; set; }
        
        //Viewer
        public Guid ViewerId { get; set; }
        public Guid CreatorId { get; set; }

        [Required]
        public string? CreatorUsername { get; set; }

        [Required]
        public string? CreatorAvatarUrl { get; set; }
    }
}
