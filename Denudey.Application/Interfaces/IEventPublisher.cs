using Denudey.Api.Domain.Events;

namespace Denudey.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    }
}
