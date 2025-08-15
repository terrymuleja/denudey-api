using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Entities;

namespace Denudey.Application.Interfaces
{
    public interface IOrdersService
    {
        Task<IEnumerable<UserRequest>> GetOrdersForCreatorAsync(Guid creatorId);
        Task<UserRequest> AcceptRequestAsync(Guid requestId, Guid creatorId);

        Task<UserRequest> DeliverRequestAsync(Guid requestId, Guid creatorId, DeliverRequestDto requestDto);

        Task<UserRequest> ValidateRequestAsync(Guid requestId, RequestValidationResult validation);

        Task<IEnumerable<UserRequest>> BulkValidateRequestsAsync(IEnumerable<ValidationRequestDto> validations);

    }
}
