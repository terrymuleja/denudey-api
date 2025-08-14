using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;

namespace Denudey.Application.Interfaces
{
    public interface IUserRequestService
    {
        // CREATE
        Task<UserRequest> CreateRequestAsync(CreateUserRequestDto request, Guid currentId);

        // READ
        Task<UserRequest> GetRequestByIdAsync(Guid requestId);
        Task<UserRequest?> GetRequestByIdOrNullAsync(Guid requestId);
        Task<IEnumerable<UserRequest>> GetAllRequestsAsync();
        Task<IEnumerable<UserRequest>> GetRequestsForCreatorAsync(Guid creatorId);
        Task<IEnumerable<UserRequest>> GetRequestsForRequesterAsync(Guid requestorId);
        Task<IEnumerable<UserRequest>> GetRequestsByStatusAsync(UserRequestStatus status);
        Task<IEnumerable<UserRequest>> GetRequestsByDeadLineAsync(DeadLine deadLine);
        Task<IEnumerable<UserRequest>> GetPendingValidationRequestsAsync();
        Task<IEnumerable<UserRequest>> GetExpiredRequestsAsync();
        Task<(IEnumerable<UserRequest> requests, int totalCount)> GetRequestsPagedAsync(int page, int pageSize);

        // UPDATE
        Task<UserRequest> UpdateRequestAsync(Guid requestId, UpdateUserRequestDto updateDto);
        Task<UserRequest> AcceptRequestAsync(Guid requestId, Guid creatorId);
        Task<UserRequest> DeliverRequestAsync(Guid requestId, Guid creatorId, string imageUrl);
        Task<UserRequest> ValidateRequestAsync(Guid requestId, RequestValidationResult validation);
        Task<UserRequest> UpdateStatusAsync(Guid requestId, UserRequestStatus status);
        Task<UserRequest> CancelRequestAsync(Guid requestId, Guid userId);
        Task<UserRequest> ExpireRequestAsync(Guid requestId);

        // BULK OPERATIONS
        Task<int> BulkUpdateExpiredRequestsAsync();
        Task<IEnumerable<UserRequest>> BulkValidateRequestsAsync(IEnumerable<ValidationRequestDto> validations);

        // QUERY HELPERS
        Task<bool> ExistsAsync(Guid requestId);
        Task<int> GetTotalCountAsync();
        Task<int> GetCountByStatusAsync(UserRequestStatus status);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetRevenueByCreatorAsync(Guid creatorId);
    }
}
