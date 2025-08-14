using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;

using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.DTOs.Requests;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Security;

namespace Denudey.Api.Application.Services
{
    public class UserRequestService : IUserRequestService
    {
        private readonly StatsDbContext _context;
        private readonly IWalletService _walletService;
        private readonly IProductSearchIndexer _productSearchIndexer;
        private readonly ILogger<UserRequestService> _logger;

        public UserRequestService(
            StatsDbContext context,
            IWalletService walletService,
            IProductSearchIndexer productService,
            ILogger<UserRequestService> logger)
        {
            _context = context;
            _walletService = walletService;
            _productSearchIndexer = productService;
            _logger = logger;
        }

        #region CREATE

        public async Task<UserRequest> CreateRequestAsync(CreateUserRequestDto request, Guid currentId)
        {
            try
            {
                // Calculate total cost based on deadline
                var (totalCost, extraCost) = CalculateCost(request.DeadLine);

                // Check if user has sufficient gems
                var userWallet = await _walletService.GetWalletAsync(request.RequestorId);
                if (userWallet.GemBalance < totalCost)
                {
                    throw new InsufficientFundsException("Not enough gems to create request");
                }

                // Get product details from ElasticSearch
                var product = await _productSearchIndexer.GetProductByIdAsync(request.ProductId, currentId);
                if (product == null)
                {
                    throw new DenudeyNotFoundException("Product not found");
                }

                
                // Create the request
                var userRequest = new UserRequest
                {
                    Id = Guid.NewGuid(),
                    RequestorId = request.RequestorId,
                    ProductId = request.ProductId,
                    CreatorId = product.CreatedBy,
                    ProductName = product.ProductName,
                    MainPhotoUrl = product.MainPhotoUrl,
                    BodyPart = product.BodyPart,
                    Text = request.Text ?? string.Empty,
                    DeliveredImageUrl = string.Empty,
                    PriceAmount = 3m, // Base price
                    ExtraAmount = extraCost,
                    TotalAmount = totalCost,
                    Tax = CalculateTax(totalCost),
                    Status = UserRequestStatus.Pending,
                    DeadLine = request.DeadLine,
                    
                    DeliveredDate = null,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    BodyPartValidated = null,
                    TextValidated = null,
                    ManualValidated = null
                };

                _context.UserRequests.Add(userRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created user request {RequestId} for user {UserId}",
                    userRequest.Id, request.RequestorId);

                return userRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user request for user {UserId}", request.RequestorId);
                throw;
            }
        }

        #endregion

        #region READ

        public async Task<UserRequest> GetRequestByIdAsync(Guid requestId)
        {
            var request = await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .FirstOrDefaultAsync(ur => ur.Id == requestId);

            if (request == null)
            {
                throw new DenudeyNotFoundException($"User request with ID {requestId} not found");
            }

            return request;
        }

        public async Task<UserRequest?> GetRequestByIdOrNullAsync(Guid requestId)
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .FirstOrDefaultAsync(ur => ur.Id == requestId);
        }

        public async Task<IEnumerable<UserRequest>> GetAllRequestsAsync()
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRequest>> GetRequestsForCreatorAsync(Guid creatorId)
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Where(ur => ur.CreatorId == creatorId)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRequest>> GetRequestsForRequesterAsync(Guid requestorId)
        {
            var data = await _context.UserRequests
                .Include(ur => ur.Creator)
                .Where(ur => ur.RequestorId == requestorId)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
            return data;
        }

        public async Task<IEnumerable<UserRequest>> GetRequestsByStatusAsync(UserRequestStatus status)
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .Where(ur => ur.Status == status)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRequest>> GetRequestsByDeadLineAsync(DeadLine deadLine)
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .Where(ur => ur.DeadLine == deadLine)
                .OrderByDescending(ur => ur.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRequest>> GetPendingValidationRequestsAsync()
        {
            return await _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .Where(ur => ur.Status == UserRequestStatus.Delivered &&
                           ur.BodyPartValidated == null &&
                           ur.TextValidated == null &&
                           ur.ManualValidated == null)
                .OrderBy(ur => ur.DeliveredDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRequest>> GetExpiredRequestsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.UserRequests
                .Where(ur => ur.Status == UserRequestStatus.Accepted &&
                           ur.ExpectedDeliveredDate.HasValue &&
                           ur.ExpectedDeliveredDate.Value < now)
                .ToListAsync();
        }

        public async Task<(IEnumerable<UserRequest> requests, int totalCount)> GetRequestsPagedAsync(int page, int pageSize)
        {
            var query = _context.UserRequests
                .Include(ur => ur.Requester)
                .Include(ur => ur.Creator)
                .OrderByDescending(ur => ur.CreatedAt);

            var totalCount = await query.CountAsync();
            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (requests, totalCount);
        }

        #endregion

        #region UPDATE

        public async Task<UserRequest> UpdateRequestAsync(Guid requestId, UpdateUserRequestDto updateDto)
        {
            var request = await GetRequestByIdAsync(requestId);

            // Only allow updates if request is in Pending status
            if (request.Status != UserRequestStatus.Pending)
            {
                throw new InvalidOperationException("Can only update pending requests");
            }

            // Update allowed fields
            if (!string.IsNullOrEmpty(updateDto.Text))
                request.Text = updateDto.Text;

            if (updateDto.DeadLine.HasValue)
            {
                request.DeadLine = updateDto.DeadLine.Value;
                

                // Recalculate costs if deadline changed
                var (totalCost, extraCost) = CalculateCost(updateDto.DeadLine.Value);
                request.ExtraAmount = extraCost;
                request.TotalAmount = totalCost;
                request.Tax = CalculateTax(totalCost);
            }

            request.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user request {RequestId}", requestId);
            return request;
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

        public async Task<UserRequest> DeliverRequestAsync(Guid requestId, Guid creatorId, string imageUrl)
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
                request.DeliveredImageUrl = imageUrl;
                request.DeliveredDate = DateTime.UtcNow;
                request.Status = UserRequestStatus.Delivered;
                request.ModifiedAt = DateTime.UtcNow;

                // Move gems to Creator 
                var description = $"Delivery: order '{request.BodyPart}-{request.Text}' to '{request.Requester.Username}'";
                await _walletService.AddGemsAsync(request.CreatorId, request.TotalAmount, description);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Request {RequestId} delivered with image {ImageUrl}",
                    requestId, imageUrl);

                return request;
            });
            
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

        public async Task<UserRequest> UpdateStatusAsync(Guid requestId, UserRequestStatus status)
        {
            var request = await GetRequestByIdAsync(requestId);

            request.Status = status;
            request.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} status updated to {Status}", requestId, status);
            return request;
        }

        public async Task<UserRequest> CancelRequestAsync(Guid requestId, Guid userId)
        {
            var request = await GetRequestByIdAsync(requestId);

            // Only requester can cancel their own request
            if (request.RequestorId != userId)
            {
                throw new UnauthorizedAccessException("Not authorized to cancel this request");
            }

            if (request.Status != UserRequestStatus.Pending)
            {
                throw new InvalidOperationException("Cannot cancel request in current status");
            }

            if (request.Status != UserRequestStatus.Pending)
            {
                throw new InvalidOperationException("Cannot cancel request in current status");
            }

            request.Status = UserRequestStatus.Cancelled;
            request.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} cancelled by user {UserId}", requestId, userId);
            return request;
        }

        public async Task<UserRequest> ExpireRequestAsync(Guid requestId)
        {
            var request = await GetRequestByIdAsync(requestId);

            // Refund gems to user if request was accepted
            if (request.Status == UserRequestStatus.Accepted)
            {
                var description = "Refund - Request Expired";
                await _walletService.AddGemsAsync(request.RequestorId, request.TotalAmount, description);
            }

            request.Status = UserRequestStatus.Expired;
            request.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} expired and refunded", requestId);
            return request;
        }

        #endregion

        #region BULK OPERATIONS

        public async Task<int> BulkUpdateExpiredRequestsAsync()
        {
            var expiredRequests = await GetExpiredRequestsAsync();
            var count = 0;

            foreach (var request in expiredRequests)
            {
                await ExpireRequestAsync(request.Id);
                count++;
            }

            _logger.LogInformation("Bulk expired {Count} requests", count);
            return count;
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

        #endregion

        #region QUERY HELPERS

        public async Task<bool> ExistsAsync(Guid requestId)
        {
            return await _context.UserRequests.AnyAsync(ur => ur.Id == requestId);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.UserRequests.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(UserRequestStatus status)
        {
            return await _context.UserRequests.CountAsync(ur => ur.Status == status);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.UserRequests
                .Where(ur => ur.Status == UserRequestStatus.Paid)
                .SumAsync(ur => ur.TotalAmount);
        }

        public async Task<decimal> GetRevenueByCreatorAsync(Guid creatorId)
        {
            return await _context.UserRequests
                .Where(ur => ur.CreatorId == creatorId && ur.Status == UserRequestStatus.Paid)
                .SumAsync(ur => ur.TotalAmount);
        }

        #endregion

        #region PRIVATE HELPERS

        private (decimal totalCost, decimal extraCost) CalculateCost(DeadLine deadLine)
        {
            return deadLine switch
            {
                DeadLine.ThreeDays => (3m, 0m),
                DeadLine.Express48h => (4m, 1m),
                DeadLine.Express24h => (5m, 2m),
                _ => (3m, 0m)
            };
        }

        private DateTime CalculateExpectedDeliveryDate(DeadLine deadLine, DateTime? dateAccepted)
        {
            return deadLine switch
            {
                DeadLine.ThreeDays => dateAccepted?.AddDays(3) ?? DateTime.Now.AddDays(5), // 3-5 days, use 5 as max
                DeadLine.Express48h => dateAccepted?.AddHours(48) ?? DateTime.Now.AddHours(48),
                DeadLine.Express24h => dateAccepted?.AddHours(2) ?? DateTime.Now.AddHours(2),
                _ => dateAccepted?.AddDays(5) ?? DateTime.Now.AddDays(5)
            };
        }
        private decimal CalculateTax(decimal amount)
        {
            // Implement your tax calculation logic
            // Example: 10% tax
            return amount * 0.10m;
        }

        #endregion
    }
}