using System.Security.Claims;
using Denudey.Api.Domain.DTOs.Requests;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;
using Denudey.Application.Interfaces;
using Denudey.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DenudeyControlerBase: ControllerBase
    {
        protected readonly ISocialService _socialService;
        protected readonly ILogger _logger;
        public DenudeyControlerBase(ISocialService socialService, ILogger logger)
        {
            _socialService = socialService;
            _logger = logger;
        }
        protected Guid GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
                throw new UnauthorizedAccessException("Invalid user ID.");
            return guid;
        }

        protected string? GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value;
        }

        protected string MapDeadlineToString(DeadLine deadline)
        {
            return deadline switch
            {
                DeadLine.ThreeDays => "3d",
                DeadLine.Express48h => "48h",
                DeadLine.Express24h => "24h",
                _ => "3d"
            };
        }

        protected string GetValidationStatus(UserRequest request)
        {
            if (request.BodyPartValidated == null && request.TextValidated == null && request.ManualValidated == null)
                return "pending";

            if (request.ManualValidated == true)
                return "approved";

            if (request.ManualValidated == false)
                return "rejected";

            if (request.BodyPartValidated == true && request.TextValidated == true)
                return "approved";

            if (request.BodyPartValidated == false || request.TextValidated == false)
                return "rejected";

            return "pending";
        }


        protected async Task<UserRequestResponseDto> MapToDtoAsync(UserRequest request)
        {

            var u = await _socialService.GetUserSocialProfileAsync(request.CreatorId, "model");
            if (u != null)
            {
                var userSocial = (CreatorSocial)u;
                return new UserRequestResponseDto
                {
                    Id = request.Id,
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    BodyPart = request.BodyPart,
                    CreatorId = request.CreatorId,
                    CreatorUsername = userSocial?.Username ?? "",
                    MainPhotoUrl = request.MainPhotoUrl,
                    RequestorId = request.RequestorId,
                    Text = request.Text,
                    Status = request.Status.ToString().ToLower(),
                    Deadline = MapDeadlineToString(request.DeadLine),
                    PriceAmount = request.PriceAmount,
                    ExtraAmount = request.ExtraAmount,
                    TotalAmount = request.TotalAmount,
                    Tax = request.Tax,
                    DeliveredImageUrl = request.DeliveredImageUrl,
                    ExpectedDeliveredDate = request.ExpectedDeliveredDate,
                    DeliveredDate = request.DeliveredDate,
                    CreatedAt = request.CreatedAt,
                    ModifiedAt = request.ModifiedAt,
                    ValidationStatus = GetValidationStatus(request),
                    AcceptedAt = request.AcceptedAt,

                };

            }
            throw new Exception("User not found");
        }


    }
}
