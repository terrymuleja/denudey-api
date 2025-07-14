namespace Denudey.Api.Models.DTOs;

public record SessionDto(
    string DeviceId,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? Revoked
);