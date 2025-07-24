using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Events
{
    public class EpisodeLikedEvent : DomainEvent
    {
        public Guid LikerId { get; init; }
        public int EpisodeId { get; init; }
        public Guid CreatorId { get; init; }
    }
}
