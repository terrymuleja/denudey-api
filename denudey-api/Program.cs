﻿using Denudey.Api.Interfaces;
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
using Microsoft.EntityFrameworkCore;
using Denudey.Application.Interfaces;


namespace denudey_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            builder.Services.AddScoped<IEventPublisher, EventPublisher>();
            builder.Services.AddScoped<EpisodeService>();
            builder.Services.AddScoped<EpisodeQueryService>();

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
