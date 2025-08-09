using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Entities;

namespace Denudey.Application.Interfaces
{
    public interface ISocialService
    {
        /// <summary>
        /// Updates or creates social profile in statsDb based on user data from main DB
        /// </summary>
        /// <param name="user">The user entity from ApplicationDbContext</param>
        /// <param name="role">The current role (requester/model)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task UpdateUserSocialProfileAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user's social profile from statsDb based on their role
        /// </summary>
        /// <param name="userId">The user's unique identifier</param>
        /// <param name="role">The user's current role</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The social profile or null if not found</returns>
        Task<object?> GetUserSocialProfileAsync(Guid userId, string role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes social profiles when user changes roles
        /// </summary>
        /// <param name="userId">The user's unique identifier</param>
        /// <param name="excludeRole">Role to exclude from deletion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task RemoveOtherSocialProfilesAsync(Guid userId, string excludeRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all social profiles for a user (e.g., when user is deleted)
        /// </summary>
        /// <param name="userId">The user's unique identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task RemoveAllSocialProfilesAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
