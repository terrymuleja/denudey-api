namespace Denudey.Api.Models.DTOs;

public record RegisterRequest(string Email, string Password, string? DeviceId, string Role);