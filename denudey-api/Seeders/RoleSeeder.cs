using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DenudeyApi.Seeders;

public static class RoleSeeder
{
    private static readonly string[] DefaultRoles = new[] { "Model", "Requester", "Admin" };

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        foreach (var roleName in DefaultRoles)
        {
            var exists = await db.Roles.AnyAsync(r => r.Name == roleName);
            if (!exists)
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }

        await db.SaveChangesAsync();
    }
}