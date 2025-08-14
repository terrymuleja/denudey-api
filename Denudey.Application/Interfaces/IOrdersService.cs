using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Entities;

namespace Denudey.Application.Interfaces
{
    public interface IOrdersService
    {

        Task<IEnumerable<UserRequest>> GetOrdersForCreatorAsync(Guid creatorId);

    }
}
