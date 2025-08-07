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
    [Authorize(Roles = "model")]
    public class ProductsController (IProductsService service,
        ProductQueryService productQueryService,
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
    }
}
