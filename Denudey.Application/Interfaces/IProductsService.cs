using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;

namespace Denudey.Application.Interfaces
{
    public interface IProductsService
    {
        Task<Product> CreateProductAsync(CreateProductDto dto, Guid userId);
        Task UpdateProductAsync(Product product, CreateProductDto dto);
        Task<Product?> GetProductAsync(Guid id);
        Task<Product?> GetProductForEditAsync(Guid id, Guid userId);
        Task PublishProductAsync(Guid id, Guid userId);
        Task UnpublishProductAsync(Guid id, Guid userId);
        Task ExpireProductAsync(Guid id, Guid userId);
        Task<List<ProductSummaryDto>> GetMyProductsAsync(Guid userId);

    }

}
