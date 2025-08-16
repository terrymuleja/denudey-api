# Dockerfile with detailed logging for Railway debugging
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# Debug: Show what we have
RUN echo "=== Listing root directory ===" && ls -la
RUN echo "=== Listing denudey-api directory ===" && ls -la denudey-api/
RUN echo "=== Checking for csproj files ===" && find . -name "*.csproj" | head -10

# Restore with error handling
RUN echo "=== Starting restore ===" && \
    dotnet restore "denudey-api/denudey-api.csproj" --verbosity normal || \
    (echo "RESTORE FAILED" && exit 1)

# Build with error handling
RUN echo "=== Starting build ===" && \
    dotnet build "denudey-api/denudey-api.csproj" -c Release -o /app/build --verbosity normal || \
    (echo "BUILD FAILED" && exit 1)

FROM build AS publish
# Publish with error handling and detailed output
RUN echo "=== Starting publish ===" && \
    dotnet publish "denudey-api/denudey-api.csproj" -c Release -o /app/publish /p:UseAppHost=false --verbosity detailed || \
    (echo "PUBLISH FAILED" && find /app -name "*.log" -exec cat {} \; && exit 1)

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "denudey-api.dll"]