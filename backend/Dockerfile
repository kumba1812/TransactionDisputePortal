# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj", "backend/TransactionDisputePortal.Api/"]

# Restore dependencies
RUN dotnet restore "backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directory for SQLite
RUN mkdir -p /app/data

# Expose port
EXPOSE 5115

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
	CMD curl -f http://localhost:5115/api/health || exit 1

# Set environment
ENV ASPNETCORE_URLS=http://+:5115
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "TransactionDisputePortal.Api.dll"]
