using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class EventPublisher : IEventPublisher
    {
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(ILogger<EventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
        {
            // For MVP, you can just log or later plug into MediatR, Kafka, etc.
            _logger.LogInformation("Event published: {@event}", @event);
            return Task.CompletedTask;
        }
    }
}
