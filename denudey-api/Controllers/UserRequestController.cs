using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;
using Denudey.Api.Domain.Exceptions;
using System.Security.Claims;
using Denudey.Application.Interfaces;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Application.Services;

namespace Denudey.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserRequestController : DenudeyControlerBase
    {
        private readonly IUserRequestService _userRequestService;
        
       
        public UserRequestController(
            IUserRequestService userRequestService,
            ISocialService socialService,
            ILogger<UserRequestController> logger):base(socialService, logger)
        {
            _userRequestService = userRequestService;
            
        }

        #region Helper Methods

        

        
        
       

        private DeadLine MapStringToDeadline(string deadline)
        {
            return deadline?.ToLower() switch
            {
                "24h" => DeadLine.Express24h,
                "48h" => DeadLine.Express48h,
                "3d" => DeadLine.ThreeDays,
                _ => DeadLine.ThreeDays
            };
        }

        #endregion

        #region Create Request

        /// <summary>
        /// Create a new product request
        /// </summary>
        /// <param name="model">Request creation data</param>
        /// <returns>Created request details</returns>
        [HttpPost]
        //[Authorize(Roles = "requester")]
        public async Task<ActionResult<CreateRequestResponse>> CreateRequest([FromBody] CreateRequestInputDto model)
        {
            try
            {
                var currentUserId = GetUserId();

                // Validate the request DTO
                if (model.ProductId == Guid.Empty)
                {
                    return BadRequest(new { error = "Product ID is required" });
                }

                if (model.CreatorId == null)
                {
                    return BadRequest(new { error = "Creator ID is required" });
                }

                if (string.IsNullOrEmpty(model.SelectedDeadline))
                {
                    return BadRequest(new { error = "Deadline is required" });
                }

                
                // Map to service DTO
                var requestDto = new CreateUserRequestDto
                {
                    RequestorId = currentUserId,
                    ProductId = model.ProductId,
                    Text = model.Message ?? string.Empty,
                    DeadLine = MapStringToDeadline(model.SelectedDeadline)
                };

                // Create the request
                var createdRequest = await _userRequestService.CreateRequestAsync(requestDto, currentUserId);

                var response = new CreateRequestResponse
                {
                    Success = true,
                    Request = await MapToDtoAsync(createdRequest)
                };

                _logger.LogInformation("Request created successfully: {RequestId} by user {UserId}",
                    createdRequest.Id, currentUserId);

                return Ok(response);
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning("Insufficient funds for user {UserId}: {Message}", GetUserId(), ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (DenudeyNotFoundException ex)
            {
                _logger.LogWarning("Product not found: {Message}", ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request for user {UserId}", GetUserId());
                return StatusCode(500, new { error = "An error occurred while creating the request" });
            }
        }

        #endregion

        #region Get Requests

        /// <summary>
        /// Get current user's requests (as requester)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <returns>List of user's requests</returns>
        [HttpGet("my-requests")]
        public async Task<ActionResult<GetRequestsResponse>> GetMyRequests(
            [FromQuery] int page = 1,
            [FromQuery] string? status = null)
        {
            try
            {
                var currentUserId = GetUserId();
                const int pageSize = 20;

                // Get all requests for the user
                var allRequests = await _userRequestService.GetRequestsForRequesterAsync(currentUserId);

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

        /// <summary>
        /// Get requests received by current user (as creator)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <returns>List of received requests</returns>
        [HttpGet("received")]
        public async Task<ActionResult<GetRequestsResponse>> GetReceivedRequests(
            [FromQuery] int page = 1,
            [FromQuery] string? status = null)
        {
            try
            {
                var currentUserId = GetUserId();
                const int pageSize = 20;

                // Get all requests received by the user
                var allRequests = await _userRequestService.GetRequestsForCreatorAsync(currentUserId);

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
                _logger.LogError(ex, "Error getting received requests for user {UserId}", GetUserId());
                return StatusCode(500, new { error = "An error occurred while retrieving received requests" });
            }
        }

        /// <summary>
        /// Get a specific request by ID
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <returns>Request details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserRequestResponseDto>> GetRequestById(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var requestId))
                {
                    return BadRequest(new { error = "Invalid request ID format" });
                }

                var currentUserId = GetUserId();
                var request = await _userRequestService.GetRequestByIdAsync(requestId);

                // Ensure user can only access their own requests (as requester or creator)
                if (request.RequestorId != currentUserId && request.CreatorId != currentUserId)
                {
                    return Forbid("You can only access your own requests");
                }
                var result = await MapToDtoAsync(request);
                return Ok(result);
            }
            catch (DenudeyNotFoundException)
            {
                return NotFound(new { error = "Request not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request {RequestId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { error = "An error occurred while retrieving the request" });
            }
        }

        #endregion

        #region Update Request Status

        /// <summary>
        /// Update request status (accept/decline by creator)
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <param name="updateStatusDto">Status update data</param>
        /// <returns>Updated request</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<UpdateRequestResponse>> UpdateRequestStatus(
            string id,
            [FromBody] UpdateRequestStatusDto updateStatusDto)
        {
            try
            {
                if (!Guid.TryParse(id, out var requestId))
                {
                    return BadRequest(new { error = "Invalid request ID format" });
                }

                var currentUserId = GetUserId();
                var request = await _userRequestService.GetRequestByIdAsync(requestId);

                // Only creator can update status
                if (request.CreatorId != currentUserId)
                {
                    return Forbid("Only the creator can update request status");
                }

                UserRequest updatedRequest;

                switch (updateStatusDto.Status.ToLower())
                {
                    case "accepted":
                        updatedRequest = await _userRequestService.AcceptRequestAsync(requestId, currentUserId);
                        break;
                    case "delivered":
                        updatedRequest = await _userRequestService.UpdateStatusAsync(requestId, UserRequestStatus.Delivered);
                        break;
                    case "validated":
                        updatedRequest = await _userRequestService.UpdateStatusAsync(requestId, UserRequestStatus.Validated);
                        break;
                    default:
                        return BadRequest(new { error = "Invalid status. Allowed values: accepted, declined, completed" });
                }

                var response = new UpdateRequestResponse
                {
                    Success = true,

                    Request = await MapToDtoAsync(updatedRequest)
                };

                _logger.LogInformation("Request {RequestId} status updated to {Status} by user {UserId}",
                    requestId, updateStatusDto.Status, currentUserId);

                return Ok(response);
            }
            catch (DenudeyNotFoundException)
            {
                return NotFound(new { error = "Request not found" });
            }
            catch (DenudeyUnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request status {RequestId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { error = "An error occurred while updating the request status" });
            }
        }

        #endregion

        #region Cancel Request

        /// <summary>
        /// Cancel a request (by requester only)
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <returns>Updated request</returns>
        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult<UpdateRequestResponse>> CancelRequest(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var requestId))
                {
                    return BadRequest(new { error = "Invalid request ID format" });
                }

                var currentUserId = GetUserId();
                var updatedRequest = await _userRequestService.CancelRequestAsync(requestId, currentUserId);

                var response = new UpdateRequestResponse
                {
                    Success = true,
                    Request = await MapToDtoAsync(updatedRequest)
                };

                _logger.LogInformation("Request {RequestId} cancelled by user {UserId}", requestId, currentUserId);

                return Ok(response);
            }
            catch (DenudeyNotFoundException)
            {
                return NotFound(new { error = "Request not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling request {RequestId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { error = "An error occurred while cancelling the request" });
            }
        }

        #endregion

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
                var request = await _userRequestService.GetRequestByIdAsync(requestId);

                // Only creator can deliver
                if (request.CreatorId != currentUserId)
                {
                    return Forbid("Only the creator can deliver this request");
                }

                var updatedRequest = await _userRequestService.DeliverRequestAsync(requestId, currentUserId, deliverDto.ImageUrl);

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