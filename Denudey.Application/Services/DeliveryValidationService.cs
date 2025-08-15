using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace Denudey.Application.Services
{
    public class DeliveryValidationService : IDeliveryValidationService
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<DeliveryValidationService> _logger;

        public DeliveryValidationService(
            IPublishEndpoint publishEndpoint,
            ILogger<DeliveryValidationService> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task TriggerValidationAsync(Guid requestId, string imageUrl, string expectedText)
        {
            try
            {
                await _publishEndpoint.Publish(new DeliveryRequestReceived
                {
                    RequestId = requestId,
                    DeliveredImageUrl = imageUrl,
                    ExpectedText = expectedText,
                    DeliveredAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = "denudey-api",
                        ["version"] = "1.0"
                    }
                });

                _logger.LogInformation("Validation triggered for request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger validation for request {RequestId}", requestId);
                throw;
            }
        }
    }
}
