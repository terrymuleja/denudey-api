using Denudey.Api.Interfaces;
using Denudey.Api.Services;
using Denudey.Api.Services.Infrastructure;
using DenudeyApi.Seeders;
using DenudeyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Microsoft.OpenApi.Models;
using Denudey.Api.Services.Implementations;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Services.Infrastructure.Sharding.Implementations;
using Denudey.Application.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Collections;

namespace denudey_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

            builder.Services.Configure<CloudinarySettings>(
                builder.Configuration.GetSection("Cloudinary"));

            // Railway needs the app to listen on port 8080
            builder.WebHost.UseUrls("http://0.0.0.0:8080");

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);
                        // Using custom execution strategy instead of built-in retry
                        npgsqlOptions.ExecutionStrategy(dependencies => new CustomNpgsqlExecutionStrategy(dependencies));
                    }
                );
            });

            builder.Services.AddDbContext<StatsDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("StatsDb"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);
                        // Using custom execution strategy instead of built-in retry
                        npgsqlOptions.ExecutionStrategy(dependencies => new CustomNpgsqlExecutionStrategy(dependencies));
                    }
                );
            });

            // ✅ Fixed Elasticsearch configuration
            builder.Services.AddSingleton(sp =>
            {
                var apiKey = builder.Configuration["ELASTICSEARCH_APIKEY"];
                var endpoint = builder.Configuration["ELASTICSEARCH_ENDPOINT"];

                Console.WriteLine("=== Environment Variables Debug ===");
                foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    Console.WriteLine($"var {env.Key}: {env.Value}");
                }
                Console.WriteLine("=== End Debug ===");

                ElasticsearchClientSettings settings;

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(endpoint))
                {
                    var uri = new Uri(endpoint);
                    settings = new ElasticsearchClientSettings(uri)
                        .Authentication(new ApiKey(apiKey))
                        .DefaultIndex("scamflix_episodes"); // ✅ Fixed: Match the index name used in your code
                }
                else
                {
                    throw new InvalidOperationException("Missing Elasticsearch API key or endpoint");
                }

                return new ElasticsearchClient(settings);
            });

            // ✅ Register ElasticIndexInitializer properly
            builder.Services.AddSingleton<ElasticIndexInitializer>();

            // Register services
            builder.Services.AddScoped<IEpisodeStatsService, EpisodeStatsService>();
            builder.Services.AddScoped<IEpisodeSearchIndexer, EpisodeSearchIndexer>();
            builder.Services.AddScoped<EpisodeService>();
            builder.Services.AddScoped<EpisodeQueryService>();
            builder.Services.AddScoped<IEventPublisher, EventPublisher>();

            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddHostedService<TokenCleanupService>();

            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IProductsService, ProductsService>();
            builder.Services.AddScoped<IShardRouter, SingleShardRouter>();

            // ✅ Add Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        )
                    };
                });

            // ✅ Add Authorization
            builder.Services.AddAuthorization();

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

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "denudey-api", Version = "v1" });

                // ✅ Add Bearer authentication
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Description = "Enter JWT Bearer token",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });
            });

            var app = builder.Build();

            // ✅ Fixed: Proper async handling for index creation
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = app.Services.CreateScope();
                        var indexInit = scope.ServiceProvider.GetRequiredService<ElasticIndexInitializer>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                        logger.LogInformation("Creating Elasticsearch index...");
                        await indexInit.CreateEpisodesIndexAsync();
                        logger.LogInformation("Elasticsearch index creation completed.");
                    }
                    catch (Exception ex)
                    {
                        using var scope = app.Services.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "Failed to create Elasticsearch index during startup");
                        // Don't throw here - let the app start even if ES indexing fails
                    }
                });
            });

            // Database seeding
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

            // ✅ Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}