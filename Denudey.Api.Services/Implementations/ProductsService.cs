using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Services.Implementations
{
    public class ProductsService(ApplicationDbContext db) : IProductsService
    {
        public async Task<int> CountActiveProductsAsync(Guid userId) =>
            await db.Products.CountAsync(p => p.CreatedBy == userId && !p.IsExpired);

        public async Task<Product?> GetProductAsync(Guid id, Guid userId) =>
            await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.CreatedBy == userId);

        public async Task<bool> CanUnpublishAsync(Guid productId, Guid userId)
        {
            return !await db.Demands.AnyAsync(d => d.ProductId == productId && d.Product.CreatedBy == userId);
        }

        public async Task<Product> CreateProductAsync(CreateProductDto dto, Guid userId)
        {
            var count = await CountActiveProductsAsync(userId);
            if (count >= 10)
                throw new InvalidOperationException("Limit of 10 active products reached.");

            var fee = count < 5 ? 0.15m : 0.25m;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                ProductName = dto.ProductName,
                Description = dto.Description,
                Tags = dto.Tags.Take(10).ToList(),
                MainPhotoUrl = dto.MainPhotoUrl,
                SecondaryPhotoUrls = dto.SecondaryPhotoUrls.Take(5).ToList(),
                BodyPart = dto.BodyPart,
                DeliveryOptions = dto.DeliveryOptions,
                FeePerDelivery = fee,
                CreatedBy = userId,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product;
        }

        public async Task UpdateProductAsync(Product product, CreateProductDto dto)
        {
            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.Tags = dto.Tags.Take(10).ToList();
            product.MainPhotoUrl = dto.MainPhotoUrl;
            product.SecondaryPhotoUrls = dto.SecondaryPhotoUrls.Take(5).ToList();
            product.BodyPart = dto.BodyPart;
            product.DeliveryOptions = dto.DeliveryOptions;
            product.ModifiedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
    }

}
