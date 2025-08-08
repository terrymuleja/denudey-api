using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    
    public class ProductsService(IShardRouter shardRouter, 
        ILogger<ProductsService> logger,
        IProductSearchIndexer productSearchIndexer) : IProductsService
    {
        public async Task<Product> CreateProductAsync(CreateProductDto dto, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);

            var count = await CountActiveProductsAsync(userId, db);
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
                DeliveryOptions = dto.Deadlines,
                FeePerDelivery = fee,
                CreatedBy = userId
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();


            // Load creator for indexing
            await db.Entry(product).Reference(e => e.Creator).LoadAsync();

            // Index episode for search - with error handling
            try
            {
                await productSearchIndexer.IndexProductAsync(product);
                logger.LogInformation("Successfully indexed Product {Id}", product.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to index product {Id} for search", product.Id);
                // Don't fail the entire operation if search indexing fails
            }
            return product;
        }

        public async Task<int> CountActiveProductsAsync(Guid userId, ApplicationDbContext db)
        {
            return await db.Products.CountAsync(p => p.CreatedBy == userId && !p.IsExpired);
        }

        public async Task<Product?> GetProductForEditAsync(Guid id, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);
            return await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.CreatedBy == userId);
        }



        public async Task<bool> CanUnpublishAsync(Guid productId, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);
            return !await db.Demands.AnyAsync(d => d.ProductId == productId && d.RequestedBy == userId);
        }



        public async Task UpdateProductAsync(Guid userId, Product product, CreateProductDto dto)
        {
            var db = shardRouter.GetDbForUser(userId);

            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.Tags = dto.Tags.Take(10).ToList();
            product.MainPhotoUrl = dto.MainPhotoUrl;
            product.SecondaryPhotoUrls = dto.SecondaryPhotoUrls.Take(5).ToList();
            product.BodyPart = dto.BodyPart;
            product.DeliveryOptions = dto.Deadlines;
            product.ModifiedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Reindex in ElasticSearch (overwrites existing document)
            try
            {
                await db.Entry(product).Reference(p => p.Creator).LoadAsync();
                await productSearchIndexer.IndexProductAsync(product);
                logger.LogInformation("Successfully reindexed Product {Id} after update", product.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reindex product {Id} after update", product.Id);
                // Don't fail the operation if search indexing fails
            }
        }

        public async Task PublishProductAsync(Guid id, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);
            var product = await GetProductForEditAsync(id, userId)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.IsPublished)
                throw new InvalidOperationException("Already published.");

            product.IsPublished = true;
            product.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Reindex in ElasticSearch (overwrites existing document)
            try
            {
                await db.Entry(product).Reference(p => p.Creator).LoadAsync();
                await productSearchIndexer.IndexProductAsync(product);
                logger.LogInformation("Successfully reindexed Product {Id} after publish", product.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reindex product {Id} after publish", product.Id);
            }
        }

        public async Task UnpublishProductAsync(Guid id, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);
            var product = await GetProductForEditAsync(id, userId)
                ?? throw new KeyNotFoundException("Product not found.");

            if (!product.IsPublished)
                throw new InvalidOperationException("Product is not published.");

            var canUnpublish = await CanUnpublishAsync(id, userId);
            if (!canUnpublish)
                throw new InvalidOperationException("Cannot unpublish: demand exists.");

            product.IsPublished = false;
            product.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Reindex in ElasticSearch (overwrites existing document)
            try
            {
                await db.Entry(product).Reference(p => p.Creator).LoadAsync();
                await productSearchIndexer.IndexProductAsync(product);
                logger.LogInformation("Successfully reindexed Product {Id} after unpublish", product.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reindex product {Id} after unpublish", product.Id);
            }
        }

        public async Task ExpireProductAsync(Guid id, Guid userId)
        {
            var db = shardRouter.GetDbForUser(userId);
            var product = await GetProductForEditAsync(id, userId)
                ?? throw new KeyNotFoundException("Product not found.");

            if (product.IsExpired)
                throw new InvalidOperationException("Already expired.");

            product.IsExpired = true;
            product.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Reindex in ElasticSearch (overwrites existing document)
            try
            {
                await db.Entry(product).Reference(p => p.Creator).LoadAsync();
                await productSearchIndexer.IndexProductAsync(product);
                logger.LogInformation("Successfully reindexed Product {Id} after expire", product.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reindex product {Id} after expire", product.Id);
            }
        }


    }


}
