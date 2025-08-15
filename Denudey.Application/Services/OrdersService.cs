using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.Models;
using Microsoft.Extensions.Logging;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.DTOs.Requests;

namespace Denudey.Application.Services
{
    public class OrdersService(StatsDbContext context, IWalletService walletService, IDeliveryValidationService validationService, ILogger<OrdersService> logger):
        RequestManagementService<OrdersService>(context, walletService, logger), IOrdersService
    {

        public async Task<IEnumerable<UserRequest>> GetOrdersForCreatorAsync(Guid creatorId)
        {
            var data = await _context.UserRequests
                .Include(ur => ur.Creator)
                .Where(ur => ur.CreatorId == creatorId)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
            return data;
        }

        public async Task<UserRequest> AcceptRequestAsync(Guid requestId, Guid creatorId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // ALL database operations inside this block are part of the same retryable unit
                // The strategy handles transactions internally

                var request = await GetRequestByIdAsync(requestId);

                if (request.CreatorId != creatorId)
                {
                    throw new DenudeyUnauthorizedAccessException("Not authorized to accept this request");
                }

                if (request.Status != UserRequestStatus.Pending)
                {
                    throw new InvalidOperationException("Request is not in pending status");
                }
                // Calculate expected delivery date
                request.AcceptedAt = DateTime.UtcNow;
                var expectedDate = CalculateExpectedDeliveryDate(request.DeadLine, request.AcceptedAt);

                // Update request status
                request.ExpectedDeliveredDate = expectedDate;
                request.Status = UserRequestStatus.Accepted;
                request.ModifiedAt = DateTime.UtcNow;

                // Move gems to escrow (manual escrow - just deduct from wallet)
                await _walletService.DeductGemsAsync(request.RequestorId, request.TotalAmount);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Request {RequestId} accepted by creator {CreatorId}",
                    requestId, creatorId);

                return request;
            });
        }

        public async Task<UserRequest> DeliverRequestAsync(Guid requestId, Guid creatorId, DeliverRequestDto requestDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var request = await GetRequestByIdAsync(requestId);

                if (request.Status != UserRequestStatus.Accepted)
                {
                    throw new InvalidOperationException("Request is not in accepted status");
                }

                // Update request with delivery details
                request.DeliveredImageUrl = requestDto.ImageUrl;
                request.DeliveredDate = DateTime.UtcNow;
                request.Status = UserRequestStatus.Delivered;
                request.ModifiedAt = DateTime.UtcNow;

                // Move gems to Creator 
                // var description = $"Delivery: order '{request.BodyPart}-{request.Text}' to '{request.Requester.Username}'";
                // await _walletService.AddGemsAsync(request.CreatorId, request.TotalAmount, description);

                await _context.SaveChangesAsync();

                // 2. Trigger AI validation (NEW)
                await validationService.TriggerValidationAsync(
                    requestId,
                    requestDto.ImageUrl,
                    request.Text);

                _logger.LogInformation("Request {RequestId} delivered with image {ImageUrl}",
                    requestId, requestDto.ImageUrl);

                return request;
            });

        }

        

        public async Task<IEnumerable<UserRequest>> BulkValidateRequestsAsync(IEnumerable<ValidationRequestDto> validations)
        {
            var results = new List<UserRequest>();

            foreach (var validation in validations)
            {
                try
                {
                    var result = await ValidateRequestAsync(validation.RequestId, validation.ValidationResult);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating request {RequestId} in bulk operation", validation.RequestId);
                }
            }

            return results;
        }

        public async Task<UserRequest> ValidateRequestAsync(Guid requestId, RequestValidationResult validation)
        {
            var request = await GetRequestByIdAsync(requestId);

            if (request.Status != UserRequestStatus.Delivered)
            {
                throw new InvalidOperationException("Request is not in delivered status");
            }

            // Update validation results
            request.BodyPartValidated = validation.BodyPartValid;
            request.TextValidated = validation.TextValid;
            request.ManualValidated = validation.ManualOverride;
            request.ModifiedAt = DateTime.UtcNow;

            // Determine if validation passes
            var isValid = validation.IsValid || validation.ManualOverride == true;

            if (isValid)
            {
                // Release funds to creator (add gems to creator wallet)
                await _walletService.AddGemsAsync(request.CreatorId, request.TotalAmount, $"Release funds from [{request.Requester.Username}]");
                request.Status = UserRequestStatus.Paid;

                _logger.LogInformation("Request {RequestId} validated and paid", requestId);
            }
            else
            {
                // Validation failed - refund user
                await _walletService.AddGemsAsync(request.RequestorId, request.TotalAmount, "Validation failed - Refund requester");
                request.Status = UserRequestStatus.Dispute;

                _logger.LogInformation("Request {RequestId} validation failed - refunded to user", requestId);
            }

            await _context.SaveChangesAsync();
            return request;
        }

    }
}
