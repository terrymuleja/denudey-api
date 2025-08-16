# Dockerfile for Railway deployment with submodules
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install git
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*

# Copy everything
COPY . .

# Initialize submodules if they exist
RUN if [ -f .gitmodules ]; then \
      git config --global --add safe.directory /src && \
      git submodule update --init --recursive; \
    fi

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