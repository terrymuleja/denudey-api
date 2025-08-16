# Clean Dockerfile for .NET 8 without git operations
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# List contents to see what we have
RUN ls -la

# Restore and build (without submodule dependencies for now)
RUN dotnet restore "denudey-api/denudey-api.csproj"
RUN dotnet build "denudey-api/denudey-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "denudey-api/denudey-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "denudey-api.dll"]