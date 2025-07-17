namespace Denudey.Api.Models.DTOs
{
    public record AuthTokenResponse(
        string AccessToken,
        string RefreshToken
    );
}
