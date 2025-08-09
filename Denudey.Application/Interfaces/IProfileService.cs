using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;

namespace Denudey.Application.Interfaces
{
    public interface IProfileService
    {
        Task<MeResponse?> GetUserProfileAsync(Guid userId, string? accessToken, CancellationToken cancellationToken = default);
        Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteProfileImageAsync(Guid userId, CancellationToken cancellationToken = default);
        Task UpdatePrivacyAsync(Guid userId, bool isPrivate, CancellationToken cancellationToken = default);
        Task UpdateRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
        Task SyncUserToSocialProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
