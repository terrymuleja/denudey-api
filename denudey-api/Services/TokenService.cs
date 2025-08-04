using Denudey.Api.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Denudey.Api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TokenService> _logger;

        // ✅ Back to natural DI pattern
        public TokenService(IConfiguration config, ILogger<TokenService> logger)
        {
            _config = config;
            _logger = logger;

            // Debug: Check if logger works in constructor
            Console.WriteLine($"🔍 TokenService constructor - Logger is null: {logger == null}");

            if (logger != null)
            {
                try
                {
                    _logger.LogInformation("🎯 TokenService constructor - Logger injection successful");
                    Console.WriteLine("✅ Constructor logging works!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Constructor logging failed: {ex.Message}");
                }
            }
        }

        public string GenerateAccessToken(string userId, string role)
        {
            // Test logging first
            _logger?.LogWarning("🔑 GenerateAccessToken called for user: {userId}", userId);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = double.Parse(_config["Jwt:ExpiresInMinutes"]);

            _logger?.LogWarning("[TokenService] 🕒 ExpiresInMinutes from config: {expiry}", expiry);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger?.LogInformation("✅ Access token generated successfully");

            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            _logger?.LogInformation("🔄 GenerateRefreshToken called");

            var refreshToken = Guid.NewGuid().ToString();

            _logger?.LogInformation("✅ Refresh token generated successfully");

            return refreshToken;
        }
    }
}