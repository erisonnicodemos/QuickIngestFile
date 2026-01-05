# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY QuickIngestFile.sln .
COPY src/QuickIngestFile.Domain/*.csproj src/QuickIngestFile.Domain/
COPY src/QuickIngestFile.Application/*.csproj src/QuickIngestFile.Application/
COPY src/QuickIngestFile.Infrastructure/*.csproj src/QuickIngestFile.Infrastructure/
COPY src/QuickIngestFile.Api/*.csproj src/QuickIngestFile.Api/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/src/QuickIngestFile.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create uploads directory
RUN mkdir -p /app/uploads

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "QuickIngestFile.Api.dll"]
