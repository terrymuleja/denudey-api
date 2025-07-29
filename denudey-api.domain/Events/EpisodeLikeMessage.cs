using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Events
{
    public class EpisodeLikeMessage: DomainEvent
    {
        public string Type { get; set; } = "like";
        public int EpisodeId { get; set; }
        public Guid UserId { get; set; }
        public Guid CreatorId { get; set; }
        public string CreatorUsername { get; set; }
        public string CreatorProfileImageUrl { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
