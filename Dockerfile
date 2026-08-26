# StoreOps API — .NET 8 multi-stage Dockerfile
# Adjust the csproj path once the solution is generated if your Api project lives elsewhere.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/StoreOps.Domain/*.csproj src/StoreOps.Domain/
COPY src/StoreOps.Application/*.csproj src/StoreOps.Application/
COPY src/StoreOps.Infrastructure/*.csproj src/StoreOps.Infrastructure/
COPY src/StoreOps.Api/*.csproj src/StoreOps.Api/
RUN dotnet restore src/StoreOps.Api/StoreOps.Api.csproj

COPY src/ src/
RUN dotnet publish src/StoreOps.Api/StoreOps.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is required by docker-compose.yml's healthcheck; the base aspnet runtime image
# doesn't include it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "StoreOps.Api.dll"]
