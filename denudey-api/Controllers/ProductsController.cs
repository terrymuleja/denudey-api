using Denudey.Api.Domain.DTOs;
using Denudey.Api.Models;
using Denudey.Api.Services;
using Denudey.Application.Interfaces;
using Denudey.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController (IProductsService service,
        ProductQueryService productQueryService,
        IProductsService productsService,
        IProductSearchIndexer productSearchIndexer,
        ILogger<ProductsController> logger) : DenudeyControlerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var userId = GetUserId();

            try
            {
                var product = await service.CreateProductAsync(dto, userId);
                return Ok(new { product.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "model")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateProductDto dto)
        {
            var userId = GetUserId();
            var product = await service.GetProductForEditAsync(id, userId);

            if (product == null)
                return NotFound();
            if (product.IsPublished)
                return BadRequest("Cannot modify a published product.");

            await service.UpdateProductAsync(userId, product, dto);
            return Ok();
        }

        [Authorize(Roles = "model")]
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            var userId = GetUserId();

            try
            {
                await service.PublishProductAsync(id, userId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "model")]
        [HttpPost("{id}/unpublish")]
        public async Task<IActionResult> Unpublish(Guid id)
        {
            var userId = GetUserId();

            try
            {
                await service.UnpublishProductAsync(id, userId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "model")]
        [HttpPost("{id}/expire")]
        public async Task<IActionResult> Expire(Guid id)
        {
            var userId = GetUserId();

            try
            {
                await service.ExpireProductAsync(id, userId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "model")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(
            [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetUserId();
            var products = await productQueryService.GetMyProducts(currentUserId, search, page, pageSize);
            return Ok(products);
        }


        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDetailsDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var userId = GetUserId();
                var result = await productSearchIndexer.SearchProductsAsync(search, userId, page, pageSize);

                // Always return 200 OK, even if no results
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error in GetAll products endpoint");

                return StatusCode(500, new
                {
                    error = "An unexpected error occurred while retrieving products"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();         

            try
            {
                var product = await productSearchIndexer.GetProductByIdAsync(id, userId);

                if (product == null)
                    return NotFound("Product not found.");

                return Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product details for {ProductId}", id);
                return StatusCode(500, "An error occurred while retrieving the product.");
            }
        }

        [HttpGet("{id}/edit")]
        public async Task<IActionResult> GetByIdForEdit(Guid id)
        {
            var userId = GetUserId();

            try
            {
                var product = await productQueryService.GetProductDetailsAsync(id, userId);

                if (product == null)
                    return NotFound("Product not found.");

                return Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product details for {ProductId}", id);
                return StatusCode(500, "An error occurred while retrieving the product.");
            }
        }

        [HttpPost(template: "{id}/like")]
        public async Task<IActionResult> ToggleLike(Guid id, [FromBody] ProductActionDto model)
        {
            try
            {
                var userId = GetUserId();
                model.UserId = userId;
                var result = await productsService.ToggleLikeAsync(model);
                return Ok(new { result.HasUserLiked, result.TotalLikes });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/view")]
        public async Task<IActionResult> TrackView(Guid id, [FromBody] ProductActionDto model)
        {
            var userId = GetUserId();
            var result = await productsService.TrackViewAsync(model);
            return result ? Ok() : BadRequest();
        }
    }
}
