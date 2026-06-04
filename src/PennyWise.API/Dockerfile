# ── Build Stage ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY PennyWise.sln ./
COPY src/PennyWise.API/PennyWise.API.csproj src/PennyWise.API/
COPY src/PennyWise.Domain/PennyWise.Domain.csproj src/PennyWise.Domain/
COPY src/PennyWise.Infrastructure/PennyWise.Infrastructure.csproj src/PennyWise.Infrastructure/
COPY tests/PennyWise.Tests/PennyWise.Tests.csproj tests/PennyWise.Tests/

RUN dotnet restore

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR /src/src/PennyWise.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime Stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PennyWise.API.dll"]
