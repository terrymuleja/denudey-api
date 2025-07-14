
using Denudey.Api.Interfaces;
using Denudey.Api.Services;
using Denudey.DataAccess;
using DenudeyApi.Seeders;
using DenudeyApi.Services;
using Microsoft.EntityFrameworkCore;

namespace denudey_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Railway needs the app to listen on port 8080
            builder.WebHost.UseUrls("http://0.0.0.0:8080");

            // Add services to the container.
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddHostedService<TokenCleanupService>();


            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Seeding roles...");
                await RoleSeeder.SeedAsync(db);
                logger.LogInformation("Seeding completed.");

            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || true)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(); // Must be placed before UseAuthorization and MapControllers

            app.UseAuthorization();

          

            app.MapControllers();

            app.Run();
        }
    }
}
