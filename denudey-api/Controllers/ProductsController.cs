using Denudey.Api.Domain.DTOs;
using Denudey.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Model")]
    public class ProductsController (IProductsService service) : DenudeyControlerBase
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
            var product = await service.GetProductAsync(id, userId);

            if (product == null)
                return NotFound();
            if (product.IsPublished)
                return BadRequest("Cannot modify a published product.");

            await service.UpdateProductAsync(product, dto);
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
        public async Task<IActionResult> GetMine()
        {
            var userId = GetUserId();
            var products = await service.GetMyProductsAsync(userId);
            return Ok(products);
        }

    }
}
