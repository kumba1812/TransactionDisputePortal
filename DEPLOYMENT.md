# Deployment Guide - Capitec Transaction Dispute Portal

## Overview

This guide provides comprehensive instructions for deploying the Capitec Transaction Dispute Portal to various environments.

## Table of Contents

1. [Local Development](#local-development)
2. [Docker Deployment](#docker-deployment)
3. [Cloud Deployment](#cloud-deployment)
4. [Production Considerations](#production-considerations)
5. [Monitoring & Troubleshooting](#monitoring--troubleshooting)

---

## Local Development

### Quick Start (PowerShell)

```powershell
# Run both backend and frontend
.\start-dev.ps1
```

### Quick Start (Bash/Linux)

```bash
# Run both backend and frontend
./start-dev.sh
```

### Manual Development

**Terminal 1 - Backend:**
```powershell
cd backend/TransactionDisputePortal.Api
dotnet run
# Available at http://localhost:5115
```

**Terminal 2 - Frontend:**
```powershell
cd frontend/TransactionDisputePortal.Web
npm install
npm run dev
# Available at http://localhost:5173
```

---

## Docker Deployment

### Prerequisites

- Docker Desktop installed and running
- Docker Compose CLI

### Build and Run

```powershell
# From project root directory
docker-compose up --build
```

This command:
1. Builds the backend .NET 10 API image
2. Builds the frontend React image
3. Creates and starts both containers
4. Sets up networking
5. Initializes the PostgresDB database

### Access the Application

- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5115

### Stop the Application

```powershell
docker-compose down
```

### Remove Volumes (Clean Everything)

```powershell
docker-compose down -v
```

### View Logs

```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
docker-compose logs -f frontend
```

### Run in Detached Mode

```powershell
docker-compose up -d --build
```

---

## Cloud Deployment

### Azure Container Instances (ACI)

```bash
# Create resource group
az group create --name tdp-rg --location eastus

# Create container registry
az acr create --resource-group tdp-rg --name tdpacr --sku Basic

# Build and push images
az acr build --registry tdpacr --image tdp-backend:latest -f backend/Dockerfile .
az acr build --registry tdpacr --image tdp-frontend:latest -f frontend/Dockerfile .

# Deploy to ACI (requires docker-compose configuration)
az container create --resource-group tdp-rg \
  --name transaction-dispute-portal \
  --image tdpacr.azurecr.io/tdp-backend:latest \
  --ports 5115 \
  --environment-variables ASPNETCORE_ENVIRONMENT=Production
```

### AWS Elastic Container Service (ECS)

1. Push images to Amazon ECR
2. Create ECS task definition
3. Create ECS service
4. Configure load balancer

### Google Cloud Run

```bash
# Enable required APIs
gcloud services enable run.googleapis.com
gcloud services enable artifactregistry.googleapis.com

# Build backend
gcloud run deploy tdp-backend \
  --source=backend/TransactionDisputePortal.Api \
  --platform=managed \
  --region=us-central1

# Build frontend
gcloud run deploy tdp-frontend \
  --source=frontend/TransactionDisputePortal.Web \
  --platform=managed \
  --region=us-central1
```

### Docker Hub

```bash
# Tag images
docker tag tdp-backend:latest <username>/tdp-backend:1.0
docker tag tdp-frontend:latest <username>/tdp-frontend:1.0

# Push to Docker Hub
docker push <username>/tdp-backend:1.0
docker push <username>/tdp-frontend:1.0
```

---

## Production Considerations

### 1. Database

**Replace PostgresDB with Production Database:**

```xml
<!-- backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.9" />
</ItemGroup>
```

Update Program.cs:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**For PostgreSQL:**
```xml
<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
</ItemGroup>
```

### 2. Authentication & Authorization

Implement JWT authentication:

```csharp
// Add to Program.cs
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = "https://your-auth-provider.com";
		options.Audience = "api://transactionportal";
	});

app.UseAuthentication();
app.UseAuthorization();
```

### 3. HTTPS/SSL

```xml
<!-- appsettings.Production.json -->
{
  "Logging": {
	"LogLevel": {
	  "Default": "Warning"
	}
  }
}
```

### 4. Environment Variables

Set these in production:

```bash
# Backend
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ASPNETCORE_Kestrel__Certificates__Default__Path=/var/secrets/certs/server.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=<password>
ConnectionStrings__DefaultConnection=<production-connection-string>

# Frontend (build-time)
VITE_API_URL=https://api.production.com
```

### 5. Logging & Monitoring

Add Application Insights:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### 6. Rate Limiting

Add rate limiting middleware:

```csharp
builder.Services.AddRateLimiter(options =>
{
	options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
	{
		options.PermitLimit = 10;
		options.Window = TimeSpan.FromMinutes(1);
	});
});

app.UseRateLimiter();
```

### 7. CORS Configuration for Production

```csharp
builder.Services.AddCors(options =>
{
	options.AddPolicy("Production", policy =>
	{
		policy.WithOrigins("https://yourdomain.com")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	});
});
```

---

## Monitoring & Troubleshooting

### Health Checks

```bash
# Backend health
curl http://localhost:5115/api/health

# Frontend connectivity test
curl http://localhost:5173
```

### Docker Container Logs

```bash
# View container logs
docker logs transaction-dispute-api
docker logs transaction-dispute-web

# Follow logs in real-time
docker logs -f transaction-dispute-api
```

### Common Issues

**Issue: Port 5115 already in use**

```powershell
# Find process using port
netstat -ano | findstr :5115

# Kill the process
taskkill /PID <PID> /F
```

**Issue: Database locked**

```powershell
# Remove the PostgresDB database and restart
rm backend/TransactionDisputePortal.Api/transactiondispute.db
```

**Issue: Frontend cannot connect to backend**

1. Check backend is running: `curl http://localhost:5115/api/health`
2. Check CORS configuration in Program.cs
3. Check frontend API URL in services/api.js

**Issue: Docker image build fails**

```bash
# Clear Docker cache and rebuild
docker-compose build --no-cache

# Check Docker disk space
docker system prune -a
```

### Performance Optimization

**Backend:**
- Enable response caching
- Add database query optimization
- Implement pagination for large result sets

**Frontend:**
- Enable code splitting
- Implement lazy loading
- Optimize bundle size

### Backup & Recovery

**Backup PostgresDB Database:**
```bash
# In Docker
docker exec transaction-dispute-api cp /app/data/transactiondispute.db /app/data/backup-$(date +%s).db

# Or volume backup
docker run --rm -v tdp_db-data:/data -v $(pwd):/backup alpine tar czf /backup/db-backup.tar.gz -C /data .
```

**Restore Database:**
```bash
docker run --rm -v tdp_db-data:/data -v $(pwd):/backup alpine tar xzf /backup/db-backup.tar.gz -C /data
```

---

## Performance Benchmarks

Expected Performance Metrics:

- **Backend API Response Time**: < 100ms (typical)
- **Frontend Load Time**: < 2 seconds
- **Database Queries**: < 50ms (with indexing)
- **Container Startup Time**: < 30 seconds

---

## Support & Additional Resources

- Official Documentation: Check README.md
- GitHub Issues: https://github.com/kumba1812/TransactionDisputePortal/issues
- Docker Documentation: https://docs.docker.com/
- .NET Documentation: https://learn.microsoft.com/dotnet/

---

## Rollback Procedures

**Docker Deployment:**
```bash
# Rollback to previous image version
docker-compose down
docker rmi transactiondisputeportal-backend:old
docker-compose up -d  # This pulls the previous version
```

---

Last Updated: January 2024
