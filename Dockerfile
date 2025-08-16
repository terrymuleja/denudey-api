# Simplified Dockerfile for Railway deployment with submodules
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything (Railway automatically includes submodules)
COPY . .

# List contents to debug
RUN ls -la
RUN ls -la delivery-validation-service/ || echo "delivery-validation-service not found"

# Restore and build
RUN dotnet restore "denudey-api/denudey-api.csproj"
RUN dotnet build "denudey-api/denudey-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "denudey-api/denudey-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "denudey-api.dll"]