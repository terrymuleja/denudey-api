# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base


# Accept variables from Railway
ARG ELASTICSEARCH_APIKEY
ARG ELASTICSEARCH_ENDPOINT

# Pass them into the container
ENV ELASTICSEARCH_APIKEY=${ELASTICSEARCH_APIKEY}
ENV ELASTICSEARCH_ENDPOINT=${ELASTICSEARCH_ENDPOINT}

WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["denudey-api/denudey-api.csproj", "denudey-api/"]
RUN dotnet restore "./denudey-api/denudey-api.csproj"
COPY . .
WORKDIR "/src/denudey-api"
RUN dotnet build "./denudey-api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./denudey-api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "denudey-api.dll"]