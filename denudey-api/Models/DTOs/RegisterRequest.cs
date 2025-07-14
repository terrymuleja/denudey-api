namespace Denudey.Api.Models.DTOs;

public record RegisterRequest(string Username, string Email, string Password, string? DeviceId);