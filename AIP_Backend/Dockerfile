# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AIPBackend.csproj .
RUN dotnet restore

COPY . .
# Publish only the API project — do not build the solution (tests would need a separate restore).
RUN dotnet publish AIPBackend.csproj -c Release -o /app/publish --no-restore

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080

# ASP.NET Core listens on 8080 by default in the runtime image
ENTRYPOINT ["dotnet", "AIPBackend.dll"]
