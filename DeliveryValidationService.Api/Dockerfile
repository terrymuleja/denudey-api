# Dockerfile for DeliveryValidationService.Api
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire solution (needed for shared projects)
COPY . .

# Debug: Show structure
RUN echo "=== Listing root directory ===" && ls -la
RUN echo "=== DeliveryValidationService.Api directory ===" && ls -la DeliveryValidationService.Api/

# Restore the delivery service
RUN dotnet restore "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" --verbosity normal

# Build the delivery service
RUN dotnet build "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" -c Release -o /app/build --verbosity normal

FROM build AS publish
# Publish the delivery service
RUN dotnet publish "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --verbosity detailed

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeliveryValidationService.Api.dll"]