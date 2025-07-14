namespace Denudey.DataAccess.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public DateTime? Revoked { get; set; }

    public string? DeviceId { get; set; }

    public string? RevokedBy { get; set; } // "user", "admin", "system"

    // Navigation
    public ApplicationUser? User { get; set; }
}