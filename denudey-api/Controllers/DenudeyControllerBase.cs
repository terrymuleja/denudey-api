using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DenudeyControlerBase: ControllerBase
    {
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

    }
}
