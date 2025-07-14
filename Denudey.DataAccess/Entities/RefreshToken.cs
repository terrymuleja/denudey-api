namespace Denudey.DataAccess.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool Revoked { get; set; } = false;

    public string? DeviceId { get; set; }


    // Navigation
    public ApplicationUser? User { get; set; }
}