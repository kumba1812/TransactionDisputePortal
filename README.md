# Capitec Transaction Dispute Portal

A full-stack web application for managing transaction disputes. Built with .NET 10 (backend API) and React + Vite (frontend), allowing customers to view transactions, file disputes, and track dispute history.

## 🎯 Features

- **Login & Authentication**: JWT Bearer authentication with username/password; tokens stored in `sessionStorage`
- **Role-Based Access Control**: Four roles — Admin, Banker, Client, ReadOnly — each with distinct permissions
- **5-Minute Inactivity Logout**: Automatic session expiry after 5 minutes of user inactivity
- **View Transactions**: Display transactions filtered by the logged-in customer
- **Dispute Status Badge**: Each transaction row shows its current dispute status at a glance
- **File Disputes**: Clients submit disputes with reason and description; form is hidden once a dispute is active
- **Dispute Status Card**: Clients can see the full status, reason, and resolution notes of an existing dispute
- **Banker Dispute Management**: Bankers acquire a soft lock before editing a dispute to prevent concurrent edits
- **Dispute History**: Track all filed disputes with status and resolution details
- **Responsive Design**: Mobile-friendly interface
- **Production-Ready**: Docker containerization with PostgreSQL and deployment-ready configuration

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

### Authentication
- `POST /api/auth/login` - Authenticate and receive a JWT (pass `{"username": "...", "password": "..."}`)

### Transactions *(require Bearer token)*
- `GET /api/transactions` - Get transactions (Clients see their own; Bankers/Admin see all)
- `GET /api/transactions/{id}` - Get a specific transaction
- `POST /api/transactions` - Create a new transaction (Admin/Banker)
- `PUT /api/transactions/{id}` - Update a transaction (Admin/Banker)
- `DELETE /api/transactions/{id}` - Delete a transaction (Admin only)

### Disputes *(require Bearer token)*
- `GET /api/disputes` - Get disputes (role-filtered)
- `GET /api/disputes/{id}` - Get a specific dispute
- `GET /api/disputes/transaction/{transactionId}` - Get disputes for a transaction
- `POST /api/disputes` - File a new dispute (Client only)
- `PUT /api/disputes/{id}` - Update dispute status/resolution (Banker/Admin)
- `DELETE /api/disputes/{id}` - Delete a dispute (Admin only)
- `POST /api/disputes/{id}/lock` - Acquire soft edit lock (Banker/Admin)
- `DELETE /api/disputes/{id}/lock` - Release soft edit lock (Banker/Admin)

### Health Check
- `GET /api/health` - API health status

## 📊 Data Models

### User
```json
{
  "id": 4,
  "username": "client",
  "fullName": "Client User",
  "role": "Client",
  "isActive": true
}
```

### Transaction
```json
{
  "id": 1,
  "customerId": 4,
  "transactionUid": "TXN001",
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
# 1. Login and get a token
$response = curl -X POST http://localhost:5115/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{"username": "client", "password": "Client123!"}'
# Copy the token from the response

# 2. Get transactions (Bearer token required)
curl http://localhost:5115/api/transactions `
  -H "Authorization: Bearer <your-token>"

# 3. File a dispute
curl -X POST http://localhost:5115/api/disputes `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer <your-token>" `
  -d '{"transactionId": 3, "reason": "Unauthorized Transaction", "description": "I did not authorize this payment."}'

# 4. Get health status (no auth required)
curl http://localhost:5115/api/health
```

Or use the included `API_TESTING.http` file with VS Code REST Client (no manual token handling needed).

## � User Roles & Login

The application uses JWT Bearer authentication. All endpoints (except `/api/health`) require a valid token.

### Seeded Users

| Username | Password | Role | Access |
|---|---|---|---|
| admin | Admin123! | Admin | Full access — all endpoints |
| banker | Banker123! | Banker | View/edit all disputes; acquire locks |
| banker2 | Banker2123! | Banker | Same as above |
| client | Client123! | Client | Own transactions + file/view own disputes |
| readonly | Readonly123! | ReadOnly | Read-only access to all data |

### Role Permissions Summary

| Action | Admin | Banker | Client | ReadOnly |
|---|:---:|:---:|:---:|:---:|
| View transactions | ✅ All | ✅ All | ✅ Own | ✅ All |
| Create/edit transactions | ✅ | ✅ | ❌ | ❌ |
| View disputes | ✅ All | ✅ All | ✅ Own | ✅ All |
| File dispute | ✅ | ❌ | ✅ | ❌ |
| Update dispute status | ✅ | ✅ | ❌ | ❌ |
| Delete dispute | ✅ | ❌ | ❌ | ❌ |
| Acquire/release lock | ✅ | ✅ | ❌ | ❌ |

---

## 🗄️ Database

The application uses PostgreSQL 15 for data persistence:
- **Local**: Connect via `ConnectionStrings__DefaultConnection` (see Environment Variables below)
- **Docker**: Stored in a named volume `pgdata` for persistence across container restarts

### Database Initialization

On the first `docker-compose up`, PostgreSQL runs `backend/docker/init/seed.sql` which creates all tables, pre-seeds 5 users + sample transactions + disputes, marks the EF Core migration as already applied, and resets serial sequences.

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
