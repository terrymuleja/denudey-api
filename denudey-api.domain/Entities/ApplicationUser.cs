using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class ApplicationUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<ScamflixEpisode> Episodes { get; set; } = new List<ScamflixEpisode>();

        public string CountryCode { get; set; } = string.Empty; // e.g., "+32"
        public string Phone { get; set; } = string.Empty; // e.g., "478123456"

        public string? ProfileImageUrl { get; set; } // Cloudinary URL

        public bool IsPrivate { get; set; }

        public List<Product> Products { get; set; } = new();  // as model
        public List<Demand> Demands { get; set; } = new();    

    }
}
