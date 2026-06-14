# Architecture Documentation - Capitec Transaction Dispute Portal

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Browser                            │
│                  (http://localhost:5173)                     │
└────────────────────┬────────────────────────────────────────┘
					 │
					 │ HTTP/HTTPS
					 │
┌────────────────────▼────────────────────────────────────────┐
│               Frontend (React + Vite)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Components:                                         │   │
│  │  - TransactionsList                                 │   │
│  │  - DisputeForm                                      │   │
│  │  - DisputeHistory                                   │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Services:                                           │   │
│  │  - api.js (Axios HTTP client)                       │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
					 │
					 │ REST API (JSON)
					 │
┌────────────────────▼────────────────────────────────────────┐
│            Backend API (.NET 10)                             │
│           (http://localhost:5115)                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Controllers Layer:                                  │   │
│  │  - TransactionsController                           │   │
│  │  - DisputesController                               │   │
│  │  - HealthController                                 │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Business Logic:                                     │   │
│  │  - Repository Pattern                               │   │
│  │  - DTOs & Validation                                │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Data Access Layer:                                  │   │
│  │  - Entity Framework Core                            │   │
│  │  - ITransactionRepository                           │   │
│  │  - IDisputeRepository                               │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
					 │
					 │ SQL Queries
					 │
┌────────────────────▼────────────────────────────────────────┐
│              Database Layer (SQLite)                         │
│           (transactiondispute.db)                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Tables:                                             │   │
│  │  - Transactions                                      │   │
│  │  - Disputes                                          │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Frontend Architecture

### Technology Stack
- **Framework**: React 19.2.6
- **Build Tool**: Vite 8.0.12
- **HTTP Client**: Axios 1.7.2
- **State Management**: React Hooks (useState, useEffect)
- **Styling**: CSS3 with responsive design

### Component Hierarchy

```
App (Main Container)
├── Header
├── Navigation (Tab Switcher)
└── Main Content Area
	├── Transactions Tab
	│   ├── TransactionsList
	│   │   └── Transaction Rows
	│   └── DisputeForm (when transaction selected)
	│       ├── Form Fields
	│       └── Submit/Cancel Buttons
	└── Dispute History Tab
		└── DisputeHistory
			└── Dispute Cards (Grid)
```

### Data Flow

```
User Action → Component Event → API Call → Backend Response → State Update → Re-render
```

### Key Features

- **Lazy Loading**: Components load data on mount
- **Error Handling**: User-friendly error messages
- **Loading States**: Visual feedback during API calls
- **Responsive Design**: Mobile-first approach
- **Real-time Updates**: Refresh buttons for manual updates

---

## 2. Backend Architecture

### Technology Stack
- **Framework**: ASP.NET Core 10.0
- **ORM**: Entity Framework Core 10.0.9
- **Database**: SQLite (Development)
- **Dependency Injection**: Built-in .NET DI

### Layered Architecture

```
┌─────────────────────────────────────┐
│     Controllers (API Layer)         │
│  Handle HTTP requests/responses     │
└──────────────┬──────────────────────┘
			   │
┌──────────────▼──────────────────────┐
│   Repository Pattern (Data Layer)   │
│  Abstract database operations       │
└──────────────┬──────────────────────┘
			   │
┌──────────────▼──────────────────────┐
│  Entity Framework Core (ORM)        │
│  Database context & migrations      │
└──────────────┬──────────────────────┘
			   │
┌──────────────▼──────────────────────┐
│     SQLite Database                 │
│  Persistent data storage            │
└─────────────────────────────────────┘
```

### Controllers

#### TransactionsController
- `GET /api/transactions` - Retrieve all transactions
- `GET /api/transactions/{id}` - Get specific transaction
- `POST /api/transactions` - Create new transaction
- `PUT /api/transactions/{id}` - Update transaction
- `DELETE /api/transactions/{id}` - Delete transaction

#### DisputesController
- `GET /api/disputes` - Get all disputes
- `GET /api/disputes/{id}` - Get specific dispute
- `GET /api/disputes/transaction/{id}` - Get disputes for transaction
- `POST /api/disputes` - File new dispute
- `PUT /api/disputes/{id}` - Update dispute status
- `DELETE /api/disputes/{id}` - Delete dispute

### Models

#### Transaction Entity
```csharp
public class Transaction
{
	public int Id { get; set; }
	public int CustomerId { get; set; }
	public string TransactionId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; }
	public DateTime TransactionDate { get; set; }
	public string Merchant { get; set; }
	public string Category { get; set; }
	public TransactionStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public ICollection<Dispute> Disputes { get; set; }
}
```

#### Dispute Entity
```csharp
public class Dispute
{
	public int Id { get; set; }
	public int TransactionId { get; set; }
	public int CustomerId { get; set; }
	public string Reason { get; set; }
	public string Description { get; set; }
	public DisputeStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ResolvedAt { get; set; }
	public string? ResolutionNotes { get; set; }
	public decimal? RefundAmount { get; set; }
	public Transaction? Transaction { get; set; }
}
```

### Enums

#### TransactionStatus
- Completed
- Pending
- Failed
- Refunded

#### DisputeStatus
- Pending
- UnderReview
- Resolved
- Rejected
- Refunded

---

## 3. Database Design

### Entity Relationship Diagram (ERD)

```
┌─────────────────────────────────┐
│      Transaction                │
├─────────────────────────────────┤
│ • Id (PK)                       │
│ • CustomerId (FK)               │
│ • TransactionId (Unique)        │
│ • Amount                        │
│ • Description                   │
│ • TransactionDate               │
│ • Merchant                      │
│ • Category                      │
│ • Status                        │
│ • CreatedAt                     │
└──────────────┬──────────────────┘
			   │
			   │ 1:N
			   │
┌──────────────▼──────────────────┐
│      Dispute                    │
├─────────────────────────────────┤
│ • Id (PK)                       │
│ • TransactionId (FK)            │
│ • CustomerId (FK)               │
│ • Reason                        │
│ • Description                   │
│ • Status                        │
│ • CreatedAt                     │
│ • ResolvedAt                    │
│ • ResolutionNotes               │
│ • RefundAmount                  │
└─────────────────────────────────┘
```

### Indexes

- `Transaction.CustomerId` - For customer lookups
- `Transaction.TransactionId` - For unique identification
- `Dispute.CustomerId` - For customer disputes query
- `Dispute.TransactionId` - For transaction-related disputes
- `Dispute.Status` - For status filtering

---

## 4. API Contracts

### Request/Response Examples

#### Get Transactions Request
```http
GET /api/transactions HTTP/1.1
Host: localhost:5115
Accept: application/json
```

#### Get Transactions Response
```json
[
  {
	"id": 1,
	"customerId": 1,
	"transactionId": "TXN001",
	"amount": 125.50,
	"description": "Online Purchase",
	"transactionDate": "2024-01-15T00:00:00Z",
	"merchant": "Amazon",
	"category": "Shopping",
	"status": "Completed",
	"createdAt": "2024-01-15T10:30:00Z",
	"disputes": []
  }
]
```

#### Create Dispute Request
```json
POST /api/disputes HTTP/1.1
Host: localhost:5115
Content-Type: application/json

{
  "transactionId": 1,
  "reason": "Unauthorized",
  "description": "I did not authorize this purchase"
}
```

#### Create Dispute Response
```json
{
  "id": 1,
  "transactionId": 1,
  "customerId": 1,
  "reason": "Unauthorized",
  "description": "I did not authorize this purchase",
  "status": "Pending",
  "createdAt": "2024-01-20T10:30:00Z",
  "refundAmount": 125.50,
  "transaction": { ... }
}
```

---

## 5. Deployment Architecture

### Docker Containerization

```
┌────────────────────────────────────────────┐
│        Docker Host                         │
│  ┌──────────────────────────────────────┐  │
│  │  transaction-dispute-api             │  │
│  │  (Port 5115)                         │  │
│  │  .NET 10 ASP.NET Core                │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │  transaction-dispute-web             │  │
│  │  (Port 5173)                         │  │
│  │  React + Vite (serve)                │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │  Shared Volume: db-data              │  │
│  │  SQLite database persistence         │  │
│  └──────────────────────────────────────┘  │
└────────────────────────────────────────────┘
```

### Network Architecture

```
External Network
	 │
	 ├─→ http://localhost:5173 (Frontend)
	 │
	 └─→ http://localhost:5115 (Backend API)
		   │
		   └─→ SQLite Database
```

---

## 6. Security Architecture

### Authentication & Authorization

- **Current**: Hardcoded customer ID (1)
- **Production**: JWT token-based authentication
- **Future**: Role-based access control (RBAC)

### CORS Configuration

```csharp
// Allowed Origins in Development
- http://localhost:5173
- http://localhost:3000

// Production would be restricted to actual domain
```

### Data Validation

- Input validation at controller level
- Model state validation
- Entity-level constraints
- SQL injection prevention (via EF Core)

### Error Handling

```
Request → Validation → Processing → Response
		   ↓
	BadRequest (400)
		   ↓
	Unauthorized (401)
		   ↓
	Forbidden (403)
		   ↓
	NotFound (404)
```

---

## 7. Scalability Considerations

### Horizontal Scaling
- **Stateless API**: Each request is independent
- **Database**: Upgrade to SQL Server/PostgreSQL
- **Load Balancer**: Distribute requests across API instances
- **Cache Layer**: Add Redis for caching

### Vertical Scaling
- Increase container resources
- Optimize database queries
- Enable response caching

### Performance Optimization
- Database indexing
- Query optimization
- Client-side caching
- CDN for static assets
- Pagination for large result sets

---

## 8. Monitoring & Logging

### Application Insights Integration
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Logging Strategy
- **Info**: User actions, API calls
- **Warning**: Validation failures, missing data
- **Error**: Exceptions, database errors
- **Debug**: Detailed trace information

### Health Checks
```
GET /api/health → {"status": "healthy"}
```

---

## 9. Future Enhancements

1. **Search & Filtering**: Advanced search capabilities
2. **Notifications**: Email/SMS alerts for dispute updates
3. **Analytics**: Dashboard with dispute statistics
4. **Export**: PDF/CSV export functionality
5. **Webhooks**: Real-time event notifications
6. **Mobile App**: Native iOS/Android applications
7. **Admin Portal**: Management interface for disputes
8. **AI Integration**: Fraud detection algorithms

---

## Technology Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| React Hooks | Modern, functional approach; no class complexity |
| EF Core | Strongly-typed database access; migration support |
| SQLite (Dev) | Zero-setup, self-contained; easy for development |
| Repository Pattern | Testability; abstracts data access |
| Layered Architecture | Separation of concerns; maintainability |
| Docker | Environment consistency; easy deployment |
| Vite | Fast build times; modern tooling |
| Axios | Simplicity; interceptor support |

---

## Deployment Environments

### Development
- Local machine
- SQLite database
- Hot reload enabled
- Verbose logging

### Staging
- Docker containers
- SQL Server database
- Performance testing
- Integration testing

### Production
- Cloud infrastructure (Azure/AWS/GCP)
- Managed database service
- CDN for static assets
- Load balancing
- Monitoring & alerting

---

Last Updated: January 2024
