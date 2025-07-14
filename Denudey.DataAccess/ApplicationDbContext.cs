using Denudey.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Denudey.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    }
}
