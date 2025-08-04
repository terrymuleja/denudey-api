using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Events
{
    public class EpisodeLikedEvent : DomainEvent
    {

        public int EpisodeId { get; set; }

        public Guid LikerId { get; set; }

        public Guid CreatorId { get; set; }

        [Required]
        public string? CreatorUsername { get; set; }

        [Required]
        public string? CreatorAvatarUrl { get; set; }
    }
}
