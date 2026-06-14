# Transaction Dispute Portal

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
- Initialize the SQLite database

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

The application uses SQLite for data persistence:
- **Local**: `transactiondispute.db` in the application directory
- **Docker**: Stored in a named volume `db-data` for persistence across container restarts

### Database Initialization

The database is automatically created and seeded with sample data on first run through Entity Framework Core migrations.

## 🔒 Security Considerations

- **CORS**: Configured to allow frontend on localhost:5173
- **Authentication**: Currently uses hardcoded customer ID (1) for demo. In production, implement JWT authentication
- **Input Validation**: All endpoints validate input parameters
- **Error Handling**: Proper HTTP status codes and error messages

## 📝 Environment Variables

### Backend
```
ASPNETCORE_ENVIRONMENT=Development|Production
ASPNETCORE_URLS=http://+:5115
ConnectionStrings__DefaultConnection=Data Source=transactiondispute.db
```

### Frontend
```
VITE_API_URL=http://localhost:5115/api
```

## 🚢 Production Deployment

### Recommendations for Production:

1. **Database**: Replace SQLite with SQL Server or PostgreSQL
2. **Authentication**: Implement JWT token-based authentication
3. **SSL/TLS**: Use HTTPS in production
4. **Environment Variables**: Store sensitive data in secure configuration
5. **Logging**: Implement comprehensive logging and monitoring
6. **API Rate Limiting**: Add rate limiting to prevent abuse
7. **CORS**: Configure CORS for production domain

### Docker Production Build

```bash
docker-compose -f docker-compose.yml up -d
```

## 📦 Build & Release

### Frontend Build
```powershell
cd frontend/TransactionDisputePortal.Web
npm run build
```

Output: `dist/` directory

### Backend Build
```powershell
cd backend/TransactionDisputePortal.Api
dotnet publish -c Release -o ./publish
```

Output: `publish/` directory

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👤 Author

- **Maphandike Mtinyane** - [GitHub](https://github.com/kumba1812)

## 📞 Support

For support, email support@transactiondisputeportal.com or open an issue on GitHub.

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

### Troubleshooting

**Port Already in Use:**
```powershell
# Find process using port 5115
netstat -ano | findstr :5115

# Kill the process
taskkill /PID <PID> /F
```

**Database Issues:**
```powershell
# Delete database and let it recreate
rm transactiondispute.db
dotnet run
```

**CORS Errors:**
Check frontend is running on http://localhost:5173 and backend CORS policy includes that URL.

## 📚 Additional Resources

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vite.dev/)
- [Docker Documentation](https://docs.docker.com/)

