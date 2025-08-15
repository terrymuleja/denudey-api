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
using Denudey.Application.Services;

namespace Denudey.Api.Application.Services
{
    public class UserRequestService : RequestManagementService<UserRequestService>, IUserRequestService
    {                
        private readonly IProductSearchIndexer _productSearchIndexer;
 
        public UserRequestService(
            StatsDbContext context,
            IWalletService walletService,
            IProductSearchIndexer productService,
            ILogger<UserRequestService> logger): base(context, walletService, logger)
        {
            _productSearchIndexer = productService;
           
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

        public async Task UpdateValidationResultAsync(UpdateValidationRequest request)
        {
            var entity = await GetRequestByIdAsync(request.RequestId);

            if (entity == null)
                throw new EntityNotFoundException($"Request {request.RequestId} not found");

            // Update validation fields
            entity.ValidationStatus = request.Status;
            entity.ValidationConfidence = request.ConfidenceScore;
            entity.ValidatedAt = request.ValidatedAt;

            if (request.RequiresHumanReview)
            {
                entity.RequiresManualReview = true;
                // Maybe add to a review queue or send notification
            }

            if (!string.IsNullOrEmpty(request.ErrorMessage))
            {
                entity.ValidationError = request.ErrorMessage;
            }

            await _context.SaveChangesAsync();
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

       

        #endregion

        #region QUERY HELPERS

        

       

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

        
    }
}