namespace Denudey.Api.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(string userId);
        string GenerateRefreshToken();
    }
}