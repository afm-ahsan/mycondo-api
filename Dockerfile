# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files first for better layer caching
COPY global.json Directory.Build.props Directory.Packages.props MyCondo.slnx ./
COPY src/MyCondo.Domain/MyCondo.Domain.csproj src/MyCondo.Domain/
COPY src/MyCondo.Application/MyCondo.Application.csproj src/MyCondo.Application/
COPY src/MyCondo.Infrastructure/MyCondo.Infrastructure.csproj src/MyCondo.Infrastructure/
COPY src/MyCondo.Api/MyCondo.Api.csproj src/MyCondo.Api/
COPY src/MyCondo.Shared/MyCondo.Shared.csproj src/MyCondo.Shared/

RUN dotnet restore src/MyCondo.Api/MyCondo.Api.csproj

COPY src/ src/

RUN dotnet publish src/MyCondo.Api/MyCondo.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1000 mycondo \
    && useradd --system --uid 1000 --gid mycondo mycondo
USER mycondo

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyCondo.Api.dll"]
