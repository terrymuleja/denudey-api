using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace Denudey.Application.Services
{
    public class ValidationCompletedConsumer : IConsumer<ValidationCompleted>
    {
        private readonly ILogger<ValidationCompletedConsumer> _logger;
        private readonly IUserRequestService _requestService; // Your existing service

        public ValidationCompletedConsumer(
            ILogger<ValidationCompletedConsumer> logger,
            IUserRequestService requestService)
        {
            _logger = logger;
            _requestService = requestService;
        }

        public async Task Consume(ConsumeContext<ValidationCompleted> context)
        {
            var validation = context.Message;

            _logger.LogInformation(
                "Received validation result for request {RequestId}: {Status}",
                validation.RequestId, validation.Status);

            try
            {
                // Update your request entity
                await _requestService.UpdateValidationResultAsync(new UpdateValidationRequest
                {
                    RequestId = validation.RequestId,
                    Status = validation.Status.ToString(),
                    ConfidenceScore = validation.ConfidenceScore,
                    RequiresHumanReview = validation.RequiresHumanReview,
                    ValidatedAt = validation.ValidatedAt,
                    ErrorMessage = validation.ErrorMessage
                });

                _logger.LogInformation("Successfully processed validation for request {RequestId}", validation.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process validation result for request {RequestId}", validation.RequestId);
                throw; // This will trigger MassTransit retry
            }
        }
    }

}
