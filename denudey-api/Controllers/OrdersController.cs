using Denudey.Api.Application.Services;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.Models;
using Denudey.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(
        IOrdersService ordersService,
        ISocialService socialService,
        IRequestManagementService requestManagement,
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

        [Authorize(Roles = "model")]
        [HttpPatch("{id}/accept")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userId = GetUserId();

            try
            {
                var updatedRequest = await ordersService.AcceptRequestAsync(id, userId);

                var response = new UpdateRequestResponse
                {
                    Success = true,
                    Request = await MapToDtoAsync(updatedRequest)
                };

                return Ok(response);
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


        #region Deliver Request (Creator only)

        /// <summary>
        /// Deliver a request with image (by creator only)
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <param name="deliverDto">Delivery data</param>
        /// <returns>Updated request</returns>
        [HttpPatch("{id}/deliver")]
        public async Task<ActionResult<UpdateRequestResponse>> DeliverRequest(
            string id,
            [FromBody] DeliverRequestDto deliverDto)
        {
            try
            {
                if (!Guid.TryParse(id, out var requestId))
                {
                    return BadRequest(new { error = "Invalid request ID format" });
                }

                if (string.IsNullOrEmpty(deliverDto.ImageUrl))
                {
                    return BadRequest(new { error = "Image URL is required" });
                }

                var currentUserId = GetUserId();
                var request = await requestManagement.GetRequestByIdAsync(requestId);

                // Only creator can deliver
                if (request.CreatorId != currentUserId)
                {
                    return Forbid("Only the creator can deliver this request");
                }

                var updatedRequest = await ordersService.DeliverRequestAsync(requestId, currentUserId, deliverDto);

                var response = new UpdateRequestResponse
                {
                    Success = true,
                    Request = await MapToDtoAsync(updatedRequest)
                };

                _logger.LogInformation("Request {RequestId} delivered by user {UserId}", requestId, currentUserId);

                return Ok(response);
            }
            catch (DenudeyNotFoundException)
            {
                return NotFound(new { error = "Request not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering request {RequestId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { error = "An error occurred while delivering the request" });
            }
        }

        #endregion
    }
}
