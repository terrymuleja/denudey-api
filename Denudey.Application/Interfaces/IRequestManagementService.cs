using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;

namespace Denudey.Application.Interfaces
{
    public interface IRequestManagementService
    {
        Task<UserRequest> GetRequestByIdAsync(Guid requestId);
        Task<UserRequest> UpdateStatusAsync(Guid requestId, UserRequestStatus status);

        Task<bool> ExistsAsync(Guid requestId);

        Task<int> GetTotalCountAsync();

        Task<int> BulkUpdateExpiredRequestsAsync();
    }
}
