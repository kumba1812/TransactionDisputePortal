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
│  │  - LoginPage                                        │   │
│  │  - TransactionsList                                 │   │
│  │  - DisputeForm                                      │   │
│  │  - DisputeHistory                                   │   │
│  │  - DisputeStatusModal                               │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Context:                                            │   │
│  │  - AuthContext (JWT + sessionStorage + inactivity)  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Services:                                           │   │
│  │  - api.js (Axios + Bearer interceptor + 401 handler)│   │
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
│  │  - AuthController (login, JWT issuance)             │   │
│  │  - TransactionsController                           │   │
│  │  - DisputesController (+ lock endpoints)            │   │
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
│  │  - IUserRepository / UserRepository                 │   │
│  │  - ITransactionRepository                           │   │
│  │  - IDisputeRepository                               │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
					 │
					 │ SQL Queries
					 │
┌────────────────────▼────────────────────────────────────────┐
│              Database Layer (PostgresDB)                         │
│           (transactiondispute.db)                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Tables:                                             │   │
│  │  - Users                                             │   │
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
App (Router / AuthContext Provider)
├── LoginPage (unauthenticated route)
└── Protected Layout (requires valid JWT)
    ├── Header (username, role badge, logout)
    ├── Navigation (Tab Switcher)
    └── Main Content Area
        ├── Transactions Tab
        │   ├── TransactionsList
        │   │   └── Transaction Rows (with dispute status badge)
        │   ├── DisputeForm (when transaction selected + no active dispute)
        │   │   ├── Existing Dispute Card (shown when dispute already filed)
        │   │   └── Form Fields + Submit/Cancel
        │   └── DisputeStatusModal (Banker/Admin status editor)
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
- **Authentication**: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0)
- **Password Hashing**: PBKDF2 via `PasswordHasher<T>`
- **Authorization**: Role-based policies (Admin / Banker / Client / ReadOnly)
- **ORM**: Entity Framework Core 10.0.9 (Npgsql)
- **Database**: PostgreSQL 15
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
│     PostgreSQL 15 Database              │
│  Persistent data storage            │
└─────────────────────────────────────┘
```

### Controllers

#### AuthController
- `POST /api/auth/login` - Validate credentials, return signed JWT

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
- `POST /api/disputes/{id}/lock` - Acquire soft edit lock
- `DELETE /api/disputes/{id}/lock` - Release soft edit lock

### User Roles

| Role | Login | View Transactions | Manage Transactions | View Disputes | File Dispute | Update Dispute | Acquire Lock |
|---|---|---|---|---|---|---|---|
| Admin | ✅ | All | ✅ | All | ✅ | ✅ | ✅ |
| Banker | ✅ | All | ✅ | All | ❌ | ✅ | ✅ |
| Client | ✅ | Own | ❌ | Own | ✅ | ❌ | ❌ |
| ReadOnly | ✅ | All | ❌ | All | ❌ | ❌ | ❌ |

### Models

#### ApplicationUser Entity
```csharp
public class ApplicationUser
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }  // PBKDF2 via PasswordHasher<T>
    public string FullName { get; set; }
    public string Role { get; set; }           // "Admin" | "Banker" | "Client" | "ReadOnly"
    public bool IsActive { get; set; }
}
```

#### Transaction Entity
```csharp
public class Transaction
{
    public int Id { get; set; }
    [Column("customer_id")]      public int CustomerId { get; set; }
    [Column("transaction_uid")] public string TransactionUid { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    [Column("transaction_date")] public DateTime TransactionDate { get; set; }
    public string Merchant { get; set; }
    public string Category { get; set; }
    public TransactionStatus Status { get; set; }
    [Column("created_at")]       public DateTime CreatedAt { get; set; }
    public ICollection<Dispute> Disputes { get; set; }
}
```

#### Dispute Entity
```csharp
public class Dispute
{
    public int Id { get; set; }
    [Column("transaction_id")]   public int TransactionIdFk { get; set; }
    [Column("customer_id")]      public int CustomerId { get; set; }
    public string Reason { get; set; }
    public string Description { get; set; }
    public DisputeStatus Status { get; set; }
    [Column("created_at")]       public DateTime CreatedAt { get; set; }
    [Column("resolved_at")]      public DateTime? ResolvedAt { get; set; }
    [Column("resolution_notes")] public string? ResolutionNotes { get; set; }
    [Column("refund_amount")]    public decimal? RefundAmount { get; set; }
    // Soft-lock fields
    [Column("locked_by_user_id")] public int? LockedByUserId { get; set; }
    [Column("locked_by_name")]    public string? LockedByName { get; set; }
    [Column("locked_at")]         public DateTime? LockedAt { get; set; }
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

### Authentication Flow

```
Client                     Backend                  Database
  |                           |                         |
  |-- POST /api/auth/login --> |                         |
  |   {username, password}    |-- SELECT user WHERE --> |
  |                           |   username = ?          |
  |                           |<-- ApplicationUser -----|
  |                           |                         |
  |                           |-- VerifyHashedPassword  |
  |                           |   (PBKDF2 compare)      |
  |                           |                         |
  |<-- 200 OK {token, role} --|
  |                           |
  |-- GET /api/transactions -->|
  |   Authorization: Bearer.. |-- [Authorize(Role)] --> validate JWT claims
  |<-- 200 [transactions] ----|
```

### Login Request/Response

```http
POST /api/auth/login
Content-Type: application/json

{"username": "client", "password": "Client123!"}
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "username": "client",
  "fullName": "Client User",
  "role": "Client"
}
```

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
│  │  PostgresDB database persistence         │  │
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
		   └─→ PostgresDB Database
```

---

## 6. Security Architecture

### Authentication & Authorization

- **JWT Bearer (HS256)**: All endpoints (except `/api/health`) require `Authorization: Bearer <token>`
- **Token expiry**: 24 hours; clients must re-login after expiry or after 5-minute inactivity
- **Password storage**: PBKDF2 via `PasswordHasher<T>` (no plaintext passwords in database)
- **Session storage**: Token stored in `sessionStorage`, never `localStorage` — cleared on logout and on 401 response
- **Role enforcement**: `[Authorize(Roles = "Admin,Banker")]` on every restricted endpoint

```csharp
// Roles defined at controller/action level
[Authorize(Roles = "Admin,Banker")]   // Banker management actions
[Authorize(Roles = "Client")]          // Client-only actions (file dispute)
[Authorize(Roles = "Admin")]           // Admin-only (delete)
```

### User Roles

| Role | Description |
|---|---|
| Admin | Full access to all resources |
| Banker | View all; update/lock disputes; manage transactions |
| Client | View own transactions; file and view own disputes |
| ReadOnly | Read-only access to transactions and disputes |

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
| PostgresDB (Dev) | Zero-setup, self-contained; easy for development |
| Repository Pattern | Testability; abstracts data access |
| Layered Architecture | Separation of concerns; maintainability |
| Docker | Environment consistency; easy deployment |
| Vite | Fast build times; modern tooling |
| Axios | Simplicity; interceptor support |

---

## Deployment Environments

### Development
- Local machine
- PostgresDB database
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
