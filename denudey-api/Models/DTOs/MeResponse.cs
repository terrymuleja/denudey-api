namespace DenudeyApi.Models.DTOs;

public record MeResponse(
    Guid Id,
    string Username,
    string Role,
    string Email,
    string? CurrentDeviceId,
    DateTime? CurrentTokenExpiresAt,
    List<string> Roles,
    string CountryCode,
    string Telephone,
    string? ProfileImageUrl,
    bool IsPrivate = false
);
