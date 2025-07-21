using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;

namespace Denudey.Api.Services
{
    public interface IProductsService
    {
        Task<int> CountActiveProductsAsync(string userId);
        Task<Product?> GetProductAsync(Guid id, string userId);
        Task<bool> CanUnpublishAsync(Guid productId, string userId);
        Task<Product> CreateProductAsync(CreateProductDto dto, string userId);
        Task UpdateProductAsync(Product product, CreateProductDto dto);
    }

}
