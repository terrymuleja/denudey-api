using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;

namespace Denudey.Application.Interfaces
{
    public interface IProductStatsService
    {
        Task<Dictionary<Guid, ProductStatsDto>> GetStatsForProductsAsync(List<Guid> productIds, Guid? userId);
    }
}
