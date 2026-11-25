
# ==========================================
# Stage 1: Build
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files first (Docker caching)
COPY CMS-Backend/CMS.Api.csproj CMS-Backend/
COPY CMS.Application/CMS.Application.csproj CMS.Application/
COPY CMS.Domain/CMS.Domain.csproj CMS.Domain/
COPY CMS.Infrastructure/CMS.Infrastructure.csproj CMS.Infrastructure/

# Restore dependencies
RUN dotnet restore CMS-Backend/CMS.Api.csproj

# Copy remaining source code
COPY . .

# Publish API
RUN dotnet publish CMS-Backend/CMS.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


# ==========================================
# Stage 2: Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "CMS.Api.dll"]
