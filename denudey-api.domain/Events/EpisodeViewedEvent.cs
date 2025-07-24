

namespace Denudey.Api.Domain.Events
{
    public class EpisodeViewedEvent : DomainEvent
    {
        public Guid ViewerId { get; init; }
        public int EpisodeId { get; init; }
        public Guid CreatorId { get; init; }
    }
}
