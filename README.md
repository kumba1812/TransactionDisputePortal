# Capitec Transaction Dispute Portal

A full-stack web application for managing transaction disputes. Built with .NET 10 (backend API) and React + Vite (frontend), allowing customers to view transactions, file disputes, and track dispute history.

## 🎯 Features

- **View Transactions**: Display all customer transactions with detailed information
- **File Disputes**: Submit disputes for transactions with reason and description
- **Dispute History**: Track all filed disputes with status updates
- **Real-time Updates**: Live status tracking for disputes
- **Responsive Design**: Mobile-friendly interface
- **Production-Ready**: Docker containerization and deployment ready

## 📋 Project Structure

```
TransactionDisputePortal/
├── backend/
│   ├── TransactionDisputePortal.Api/
│   │   ├── Controllers/          # API endpoints
│   │   ├── Models/               # Domain entities
│   │   ├── Data/                 # Database context and migrations
│   │   ├── Repositories/         # Data access layer
│   │   ├── Program.cs            # Application setup
│   │   └── TransactionDisputePortal.Api.csproj
│   ├── TransactionDisputePortal.Api.Tests/  # xUnit tests
│   │   └── Helpers/              # ControllerTestHelper
│   └── Dockerfile
├── frontend/
│   ├── TransactionDisputePortal.Web/
│   │   ├── src/
│   │   │   ├── components/       # React components
│   │   │   ├── services/         # API client
│   │   │   ├── styles/           # Component styles
│   │   │   ├── App.jsx
│   │   │   └── main.jsx
│   │   ├── package.json
│   │   └── vite.config.js
│   ├── TransactionDisputePortal.Api.Tests/  # xUnit tests
│   │   └── Helpers/              # ControllerTestHelper
│   └── Dockerfile
├── docker-compose.yml
└── README.md
```

## 🚀 Getting Started

### Prerequisites

- **For Local Development:**
  - .NET 10 SDK or later
  - Node.js 18+ and npm
  - PowerShell or bash shell

- **For Docker:**
  - Docker Desktop or Docker Engine
  - Docker Compose

### Local Development Setup

#### 1. Clone the Repository

```bash
git clone https://github.com/kumba1812/TransactionDisputePortal.git
cd TransactionDisputePortal
```

#### 2. Backend Setup

```powershell
# Navigate to backend directory
cd backend/TransactionDisputePortal.Api

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the backend API (running on http://localhost:5115)
dotnet run
```

The API will be available at `http://localhost:5115`

#### 3. Frontend Setup (in a new terminal)

```powershell
# Navigate to frontend directory
cd frontend/TransactionDisputePortal.Web

# Install npm dependencies
npm install

# Start development server (running on http://localhost:5173)
npm run dev
```

The frontend will be available at `http://localhost:5173`

### Docker Deployment

#### Build and Run with Docker Compose

```powershell
# From the root directory
docker-compose up --build
```

This will:
- Build the backend .NET API image
- Build the frontend React image
- Start both services in containers
- Create a shared network for communication
- Initialize the PostgresDB database

Access the application:
- Frontend: http://localhost:5173
- Backend API: http://localhost:5115

#### Stop the Containers

```powershell
docker-compose down
```

To also remove volumes:

```powershell
docker-compose down -v
```

## 🔌 API Endpoints

### Transactions
- `GET /api/transactions` - Get all transactions for the customer
- `GET /api/transactions/{id}` - Get a specific transaction
- `POST /api/transactions` - Create a new transaction (for admin)
- `PUT /api/transactions/{id}` - Update a transaction
- `DELETE /api/transactions/{id}` - Delete a transaction

### Disputes
- `GET /api/disputes` - Get all disputes for the customer
- `GET /api/disputes/{id}` - Get a specific dispute
- `GET /api/disputes/transaction/{transactionId}` - Get disputes for a transaction
- `POST /api/disputes` - Create a new dispute
- `PUT /api/disputes/{id}` - Update dispute status
- `DELETE /api/disputes/{id}` - Delete a dispute

### Health Check
- `GET /api/health` - API health status

## 📊 Data Models

### Transaction
```csharp
{
  "id": 1,
  "customerId": 1,
  "transactionId": "TXN001",
  "amount": 125.50,
  "description": "Online Purchase",
  "transactionDate": "2024-01-15T00:00:00Z",
  "merchant": "Amazon",
  "category": "Shopping",
  "status": "Completed"
}
```

### Dispute
```csharp
{
  "id": 1,
  "transactionId": 1,
  "customerId": 1,
  "reason": "Unauthorized",
  "description": "I did not authorize this purchase",
  "status": "Pending",
  "createdAt": "2024-01-20T10:30:00Z",
  "refundAmount": 125.50
}
```

## 🧪 Testing

### Backend Unit Tests
```powershell
cd backend/TransactionDisputePortal.Api
dotnet test
```

### Frontend Tests
```powershell
cd frontend/TransactionDisputePortal.Web
npm test
```

### API Testing with curl

```powershell
# Get all transactions
curl http://localhost:5115/api/transactions

# Get health status
curl http://localhost:5115/api/health

# Create a dispute (requires transaction ID)
curl -X POST http://localhost:5115/api/disputes `
  -H "Content-Type: application/json" `
  -d '{"transactionId": 1, "reason": "Unauthorized", "description": "Test dispute"}'
```

## 🗄️ Database

The application uses PostgresDB for data persistence:
- **Local**: `transactiondispute.db` in the application directory
- **Docker**: Stored in a named volume `db-data` for persistence across container restarts

### Database Initialization

The database is automatically created and seeded with sample data on first run through Entity Framework Core migrations.

## 📝 Environment Variables

### Backend
```
ASPNETCORE_ENVIRONMENT=Development|Production
ASPNETCORE_URLS=http://+:5115
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=transactiondispute;Username=postgres;Password=postgres
```

### Frontend
```
VITE_API_URL=http://localhost:5115/api
```

## 👤 Author

- **Lawrence Maphala** - [GitHub](https://github.com/kumba1812)

## 🔄 Deployment Instructions

### Local Development
1. Follow "Local Development Setup" section above
2. Frontend: http://localhost:5173
3. Backend: http://localhost:5115

### Docker Deployment
1. Ensure Docker and Docker Compose are installed
2. Run `docker-compose up --build` from root directory
3. Frontend: http://localhost:5173
4. Backend: http://localhost:5115
