using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;


namespace Denudey.Api.Services.Infrastructure.DbContexts
{
    public class StatsDbContextFactory : IDesignTimeDbContextFactory<StatsDbContext>
    {
        public StatsDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<StatsDbContext>();
            var connectionString = config.GetConnectionString("StatsDb");

            optionsBuilder.UseNpgsql(connectionString);

            return new StatsDbContext(optionsBuilder.Options);
        }
    }
}