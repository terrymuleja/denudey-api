using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;

namespace Denudey.Application.Interfaces
{
    public interface ProductSearchIndexer
    {
        Task IndexProductAsync(Product product);
        
        Task DeleteProductFromIndexAsync(Guid productId);
        
        Task<PagedResult<Product>> SearchProductsAsync(
        string? search,
        Guid? currentUserId,
        int page,
        int pageSize);
    }

}
