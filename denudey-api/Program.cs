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
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Services.Infrastructure.Sharding.Implementations;
using Denudey.Application.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Collections;
using Denudey.Api.Application.Services;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Exceptions.DenudeyAPI.Middleware;
using MassTransit;
using DotNetEnv;

namespace denudey_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load .env file in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                Env.Load();
            }

            Console.WriteLine("=== CONSOLE TEST: Program.cs is running ===");
            Console.WriteLine($"Time: {DateTime.UtcNow}");

            var builder = WebApplication.CreateBuilder(args);

            // ✅ 1. LOGGING FIRST - with proper configuration
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            Console.WriteLine("=== Logging providers configured ===");

            // ✅ 2. CONFIGURATION SECOND
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // ✅ 3. BASIC SERVICES (that don't depend on logging)
            builder.Services.AddHttpContextAccessor();
            builder.WebHost.UseUrls("http://0.0.0.0:8080");

            // ✅ 4. CONFIGURATION BINDING
            builder.Services.Configure<CloudinarySettings>(
                builder.Configuration.GetSection("Cloudinary"));

            Console.WriteLine("=== About to configure DbContext ===");

            // ✅ 5. DATABASE CONTEXTS
            // ApplicationDbContext (already correct)            
            builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);
                        npgsqlOptions.ExecutionStrategy(dependencies => new CustomNpgsqlExecutionStrategy(dependencies));
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }
                );
            }, poolSize: 32);

            // StatsDbContext (fix this one)
            builder.Services.AddDbContextPool<StatsDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("StatsDb"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);
                        npgsqlOptions.ExecutionStrategy(dependencies => new CustomNpgsqlExecutionStrategy(dependencies));
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }
                );
            }, poolSize: 32);
            

            Console.WriteLine("=== DbContext configured ===");

            // ✅ 6. ELASTICSEARCH (SINGLETON - before scoped services)
            builder.Services.AddSingleton(sp =>
            {
                Console.WriteLine("=== Configuring Elasticsearch ===");

                var apiKey = builder.Configuration["ELASTICSEARCH_APIKEY"];
                var endpoint = builder.Configuration["ELASTICSEARCH_ENDPOINT"];

                Console.WriteLine($"API Key present: {!string.IsNullOrEmpty(apiKey)}");
                Console.WriteLine($"Endpoint: {endpoint ?? "NOT SET"}");

                ElasticsearchClientSettings settings;

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(endpoint))
                {
                    var uri = new Uri(endpoint);
                    settings = new ElasticsearchClientSettings(uri)
                        .Authentication(new ApiKey(apiKey))
                        .DefaultIndex("scamflix_episodes");

                    Console.WriteLine("Elasticsearch client configured successfully");
                }
                else
                {
                    Console.WriteLine("ERROR: Missing Elasticsearch API key or endpoint");
                    throw new InvalidOperationException("Missing Elasticsearch API key or endpoint");
                }

                return new ElasticsearchClient(settings);
            });

            builder.Services.AddSingleton<ElasticIndexInitializer>();
            builder.Services.AddSingleton<ElasticProductIndexInitializer>();

            Console.WriteLine("=== Registering SCOPED services (after logging is ready) ===");

            // ✅ 7. SCOPED SERVICES (that need logging) - Register these AFTER logging is fully configured
            builder.Services.AddScoped<ITokenService, TokenService>(); // ← This should work now!

            builder.Services.AddScoped<ISocialService, SocialService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();

            builder.Services.AddScoped<IEpisodeStatsService, EpisodeStatsService>();
            builder.Services.AddScoped<IEpisodeSearchIndexer, EpisodeSearchIndexer>();
            builder.Services.AddScoped<EpisodeService>();
            builder.Services.AddScoped<EpisodeQueryService>();

            builder.Services.AddScoped<IProductStatsService, ProductStatsService>();
            builder.Services.AddScoped<IProductSearchIndexer, ProductSearchIndexer>();
            builder.Services.AddScoped<IProductsService, ProductsService>();
            builder.Services.AddScoped<ProductQueryService>();

            builder.Services.AddScoped<IUserRequestService, UserRequestService>();
            builder.Services.AddScoped<IOrdersService, OrdersService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<IRequestManagementService, RequestManagementService<RequestManagementService<object>>>();
            

            builder.Services.AddScoped<IEventPublisher, EventPublisher>();
            builder.Services.AddScoped<IDeliveryValidationService, LocalValidationService>();

            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();            
            builder.Services.AddScoped<IShardRouter, SingleShardRouter>();

            // ✅ 8. HOSTED SERVICES (background services)
            builder.Services.AddHostedService<TokenCleanupService>();

            Console.WriteLine("=============== MassTransit ===============");
            builder.Services.AddMassTransit(x =>
            {
                // Add consumer for receiving validation results
                x.AddConsumer<ValidationCompletedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    // Get CloudAMQP URL from environment variable
                    
                    var rabbitMqUrl = builder.Configuration.GetConnectionString("RabbitMQ")
                                    ?? Environment.GetEnvironmentVariable("CLOUDAMQP_URL");

                    Console.WriteLine($"rabbitMqUrl = {rabbitMqUrl}");

                    if (string.IsNullOrEmpty(rabbitMqUrl))
                    {
                        throw new InvalidOperationException("CloudAMQP URL not found. Set CLOUDAMQP_URL environment variable.");
                    }

                    // Connect to CloudAMQP
                    cfg.Host(new Uri(rabbitMqUrl));

                    // Configure endpoint to receive validation results
                    cfg.ReceiveEndpoint("denudey-api-validation-results", e =>
                    {
                        e.ConfigureConsumer<ValidationCompletedConsumer>(context);

                        // Retry configuration
                        e.UseMessageRetry(r => r.Intervals(1000, 2000, 5000));

                        // Limit concurrent messages (CloudAMQP free tier)
                        e.ConcurrentMessageLimit = 2;
                    });

                    cfg.ConfigureEndpoints(context);
                    Console.WriteLine("MassTransit configured with RabbitMQ successfully");
                });
            });
            Console.WriteLine("=== Configuring Authentication ===");

            // ✅ 9. AUTHENTICATION & AUTHORIZATION
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
                        ),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            // ✅ 10. MVC & API SERVICES
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

            Console.WriteLine("=== Building app ===");

            var app = builder.Build();

            Console.WriteLine("=== App built successfully ===");

            // ✅ TEST NATURAL LOGGING
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("🎯 Program logger test - natural injection");

                // Test TokenService with natural DI
                Console.WriteLine("=== Testing TokenService with natural DI ===");
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                Console.WriteLine("✅ TokenService created with natural DI");
            }

            // Database seeding
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                Console.WriteLine("=== Starting database seeding ===");
                logger.LogInformation("🌱 Seeding roles...");
                await RoleSeeder.SeedAsync(db);
                Console.WriteLine("=== Database seeding completed ===");
                logger.LogInformation("✅ Seeding completed.");
            }

            // Elasticsearch startup
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                //Episodes
                Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine("=== Starting Elasticsearch index creation ===");
                        using var scope = app.Services.CreateScope();
                        var indexInit = scope.ServiceProvider.GetRequiredService<ElasticIndexInitializer>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                        logger.LogInformation("📊 Creating Elasticsearch index...");
                        await indexInit.CreateEpisodesIndexAsync();
                        logger.LogInformation("✅ Elasticsearch index creation completed.");
                    }
                    catch (Exception ex)
                    {
                        using var scope = app.Services.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "❌ Failed to create Elasticsearch index during startup");
                    }
                });

                //Products
                Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine("=== Starting Elasticsearch Product index creation ===");
                        using var scope = app.Services.CreateScope();
                        var indexInit = scope.ServiceProvider.GetRequiredService<ElasticProductIndexInitializer>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                        logger.LogInformation("📊 Creating Elasticsearch Product index...");
                        await indexInit.CreateIndexAsync();
                        logger.LogInformation("✅ Elasticsearch Product index creation completed.");
                    }
                    catch (Exception ex)
                    {
                        using var scope = app.Services.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "❌ Failed to create Elasticsearch Product index during startup");
                    }
                });
            });

            Console.WriteLine("=== Configuring middleware pipeline ===");

            if (app.Environment.IsDevelopment() || true)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            Console.WriteLine("=== 🚀 APPLICATION READY - LISTENING ON http://0.0.0.0:8080 🚀 ===");

            app.Run();
        }
    }
}