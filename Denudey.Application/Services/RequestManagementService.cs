using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.Models;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class RequestManagementService<T> : IRequestManagementService where T : class
    {
        protected readonly StatsDbContext _context;
        protected readonly ILogger<T> _logger;
        protected readonly IWalletService _walletService;
        public RequestManagementService(StatsDbContext context, IWalletService walletService, ILogger<T> logger)
        {
            _context = context;
            _logger = logger;
            _walletService = walletService;
        }
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

        public async Task<UserRequest> UpdateStatusAsync(Guid requestId, UserRequestStatus status)
        {
            var request = await GetRequestByIdAsync(requestId);

            request.Status = status;
            request.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Request {RequestId} status updated to {Status}", requestId, status);
            return request;
        }

        public async Task<int> GetCountByStatusAsync(UserRequestStatus status)
        {
            return await _context.UserRequests.CountAsync(ur => ur.Status == status);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.UserRequests.CountAsync();
        }

        public async Task<bool> ExistsAsync(Guid requestId)
        {
            return await _context.UserRequests.AnyAsync(ur => ur.Id == requestId);
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

        
        #region PRIVATE HELPERS
        protected DateTime CalculateExpectedDeliveryDate(DeadLine deadLine, DateTime? dateAccepted)
        {
            return deadLine switch
            {
                DeadLine.ThreeDays => dateAccepted?.AddDays(3) ?? DateTime.Now.AddDays(5), // 3-5 days, use 5 as max
                DeadLine.Express48h => dateAccepted?.AddHours(48) ?? DateTime.Now.AddHours(48),
                DeadLine.Express24h => dateAccepted?.AddHours(2) ?? DateTime.Now.AddHours(2),
                _ => dateAccepted?.AddDays(5) ?? DateTime.Now.AddDays(5)
            };
        }
        protected decimal CalculateTax(decimal amount)
        {
            // Implement your tax calculation logic
            // Example: 10% tax
            return amount * 0.10m;
        }

        protected (decimal totalCost, decimal extraCost) CalculateCost(DeadLine deadLine)
        {
            return deadLine switch
            {
                DeadLine.ThreeDays => (3m, 0m),
                DeadLine.Express48h => (4m, 1m),
                DeadLine.Express24h => (5m, 2m),
                _ => (3m, 0m)
            };
        }

        #endregion

    }
}
