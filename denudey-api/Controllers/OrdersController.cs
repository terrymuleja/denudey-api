using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Models;
using Denudey.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(
        IOrdersService ordersService,
        ISocialService socialService,
        ILogger<OrdersController> logger) : DenudeyControlerBase(socialService, logger)
        
    {

        [HttpGet("my-orders")]
        public async Task<ActionResult<GetRequestsResponse>> GetMyRequests(
            [FromQuery] int page = 1,
            [FromQuery] string? status = null)
        {
            try
            {
                var currentUserId = GetUserId();
                const int pageSize = 20;

                // Get all requests for the user
                var allRequests = await ordersService.GetOrdersForCreatorAsync(currentUserId);

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<UserRequestStatus>(status, true, out var statusEnum))
                    {
                        allRequests = allRequests.Where(r => r.Status == statusEnum);
                    }
                }

                // Apply pagination
                var totalCount = allRequests.Count();
                var paginatedRequests = allRequests
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();


                var requestDtos = new List<UserRequestResponseDto>();
                foreach (var request in paginatedRequests)
                {
                    var dto = await MapToDtoAsync(request);
                    requestDtos.Add(dto);
                }

                var response = new GetRequestsResponse
                {
                    Success = true,
                    Requests = requestDtos,
                    Pagination = new PaginationDto
                    {
                        Page = page,
                        PageSize = pageSize,
                        Total = totalCount,
                        HasNextPage = page * pageSize < totalCount
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests for user {UserId}", GetUserId());
                return StatusCode(500, new { error = "An error occurred while retrieving requests" });
            }
        }
    }
}
