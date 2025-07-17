namespace Denudey.Api.Models.DTOs
{
    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        string Role,
        string Name,
        string Email,
        Guid Id
    );
}
