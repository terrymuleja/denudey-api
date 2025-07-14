namespace DenudeyApi.Models.DTOs;

public record MeResponse(
    Guid Id,
    string Username,
    string Email,
    string? CurrentDeviceId,
    DateTime? CurrentTokenExpiresAt,
    List<string> Roles
);