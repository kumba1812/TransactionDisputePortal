# Capitec Transaction Dispute Portal - Project Summary

## âœ… Project Completion Status

The Capitec Transaction Dispute Portal is a production-grade full-stack application with role-based access control, dispute soft-locking for concurrency safety, and comprehensive test coverage across the backend and frontend.

### GitHub Repository
**URL**: https://github.com/kumba1812/TransactionDisputePortal

---

## ðŸ“¦ What's Included

### Backend (.NET 10)
- âœ… RESTful API with ASP.NET Core
- âœ… Entity Framework Core with PostgreSQL (Npgsql)
- âœ… Repository pattern for data access
- âœ… JWT Bearer authentication (HS256)
- âœ… Role-based access control (Admin, Banker, Client, ReadOnly)
- âœ… DB-backed user management via `ApplicationUser` model
- âœ… ASP.NET Identity `PasswordHasher<T>` for secure password hashing
- âœ… Dispute soft-locking (pessimistic concurrency) with configurable expiry
- âœ… CORS configuration for frontend integration
- âœ… Health check endpoint
- âœ… Swagger/OpenAPI documentation with Bearer auth support
- âœ… EF Core migrations (`AddUsersAndDisputeLocking`)
- âœ… Seed data (5 users, 6 transactions, 2 disputes) via `OnModelCreating`

### Frontend (React 19 + Vite)
- âœ… JWT-based auth with `AuthContext` (`tdp_token` / `tdp_user` in **sessionStorage**)
- âœ… Login page with loading and error states
- âœ… Persistent header showing logged-in user's full name and colour-coded role badge
- âœ… Auth guard â€” redirects to login when not authenticated
- âœ… Role-gated UI: Clients see "View/Dispute", Bankers/ReadOnly do not
- âœ… Dispute lock acquisition with 10-minute countdown timer
- âœ… Inline "being reviewed by [Name]" warning on 409 lock conflict
- âœ… Lock released automatically on modal close/submit
- âœ… "Filing as: [Full Name]" displayed in dispute form
- âœ… Axios request interceptor injects Bearer token; 401 response interceptor clears session
- ✅ **5-minute inactivity timeout** — session cleared automatically on mouse/keyboard/scroll idle
- ✅ **Dispute status badge** shown per transaction row (Pending / Under Review / Resolved / Refunded / Rejected)
- ✅ **Dispute detail view for clients** — status card shows reason, filed date, resolution; form hidden when dispute already exists
- âœ… Responsive design with CSS3

### Testing
- âœ… **Backend â€” 69 tests** (xUnit + Moq + EF Core InMemory)
- âœ… **Frontend â€” 41 tests** (Vitest + React Testing Library + jsdom)
- âœ… Zero failures across both suites

### Deployment & DevOps
- âœ… Dockerfile for backend (.NET 10)
- âœ… Dockerfile for frontend (React/Vite)
- âœ… `docker-compose.yml` orchestrating API + frontend + PostgreSQL
- âœ… `seed.sql` — schema with correct EF Core column names, `__EFMigrationsHistory` pre-seeded (prevents duplicate table creation), sequence resets after explicit-ID inserts

### Documentation
- âœ… **README.md** â€” Getting started guide
- âœ… **DEPLOYMENT.md** â€” Deployment procedures and cloud options
- âœ… **TESTING.md** â€” Testing strategies and examples
- âœ… **ARCHITECTURE.md** â€” System design and technical documentation

---

## ðŸ” Authentication & Authorization

### Users (Seeded)
| Username | Password | Role | Access |
|---|---|---|---|
| admin | Admin123! | Admin | Full CRUD on all resources |
| banker | Banker123! | Banker | Read all + update dispute status |
| banker2 | Banker2123! | Banker | Read all + update dispute status |
| client | Client123! | Client | Own transactions + file/delete own disputes |
| readonly | Readonly123! | ReadOnly | Read-only on all resources |

### Role Permissions
| Action | Admin | Banker | Client | ReadOnly |
|---|---|---|---|---|
| View transactions | All | All | Own only | All |
| Create transaction | âœ… | âŒ | âŒ | âŒ |
| Edit/Delete transaction | âœ… | âŒ | âŒ | âŒ |
| View disputes | All | All | Own only | All |
| File dispute | âœ… | âŒ | âœ… (own tx) | âŒ |
| Update dispute status | âœ… | âœ… (with lock) | âŒ | âŒ |
| Delete dispute | âœ… (any) | âŒ | âœ… (own only) | âŒ |
| Acquire/release lock | âœ… | âœ… | âŒ | âŒ |

---

## ðŸ”’ Dispute Soft-Locking (Concurrency Control)

Prevents two bankers from editing the same dispute simultaneously.

- **Acquire lock**: `POST /api/disputes/{id}/lock` â€” sets `LockedByUserId`, `LockedByName`, `LockedAt` on the dispute row
- **Release lock**: `DELETE /api/disputes/{id}/lock` â€” clears the three lock fields
- **Conflict detection**: If another user holds an active lock (within expiry), returns `409 Conflict` with the locker's name
- **Expiry**: Configurable via `Disputes:LockExpiryMinutes` in `appsettings.json` (default: 10 minutes)
- **Auto-expiry override**: Expired locks are silently overwritten by the next acquire request
- **Frontend countdown**: Modal shows a live `MM:SS` timer; submit is disabled when expired

---

## ðŸš€ Quick Start

### Option 1: Docker (recommended)
```powershell
docker-compose up --build
```

### Option 2: Local Development
```powershell
# Terminal 1 â€” Backend
cd backend/TransactionDisputePortal.Api
dotnet run

# Terminal 2 â€” Frontend
cd frontend/TransactionDisputePortal.Web
npm install
npm run dev
```

### Access the Application
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5115
- **Swagger UI**: http://localhost:5115/swagger

---

## ðŸ“‹ API Endpoints

### Auth
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | None | Returns JWT token |

### Transactions
| Method | Route | Roles | Description |
|---|---|---|---|
| GET | `/api/transactions` | All | Client sees own; others see all |
| GET | `/api/transactions/{id}` | All | Client cross-access â†’ 403 |
| POST | `/api/transactions` | Admin | Create transaction |
| PUT | `/api/transactions/{id}` | Admin | Update transaction |
| DELETE | `/api/transactions/{id}` | Admin | Delete transaction |

### Disputes
| Method | Route | Roles | Description |
|---|---|---|---|
| GET | `/api/disputes` | All | Client sees own; others see all |
| GET | `/api/disputes/{id}` | All | Client cross-access â†’ 403 |
| GET | `/api/disputes/transaction/{txId}` | All | Disputes for a transaction |
| POST | `/api/disputes` | Admin, Client | File dispute (own transaction only) |
| PUT | `/api/disputes/{id}` | Admin, Banker | Requires active lock; clears lock on save |
| DELETE | `/api/disputes/{id}` | Admin, Client | Client: own only; Banker: forbidden |
| POST | `/api/disputes/{id}/lock` | Admin, Banker | Acquire soft lock |
| DELETE | `/api/disputes/{id}/lock` | Admin, Banker | Release soft lock |

---

## ðŸ§ª Test Coverage

### Backend (69 tests â€” 0 failures)
| Class | Tests |
|---|---|
| `UserRepositoryTests` | 6 â€” Add, GetByUsername (found/not-found/inactive), GetById |
| `TransactionRepositoryTests` | 7 â€” Add, GetById (null), GetByCustomer, GetAll, Update, Delete |
| `DisputeRepositoryTests` | 8 â€” Add, GetAll, GetByCustomerId, GetByTransactionId, Update, Delete, UpdateLock set/clear |
| `DisputeDtoTests` | 3 â€” `IsLocked` active/expired/null |
| `AuthControllerTests` | 7 â€” Login (Admin/Banker/Client), wrong password, unknown user, empty fields |
| `TransactionsControllerTests` | 12 â€” Role-based GetAll, GetById (found/404/client-cross), Create, Update, Delete |
| `DisputesControllerTests` | 26 â€” All CRUD + lock acquire/release scenarios |

### Frontend (41 tests â€” 0 failures)
| File | Tests |
|---|---|
| `LoginPage.test.jsx` | 6 â€” Renders form, submit calls login, error on rejection, validation, loading state |
| `App.test.jsx` | 5 â€” Auth guard, main layout, full name, role badge, logout |
| `TransactionsList.test.jsx` | 6 â€” Renders rows, View/Dispute shown (Client/Admin), hidden (Banker/ReadOnly), API error |
| `DisputeHistory.test.jsx` | 8 â€” Renders cards, Update Status visibility by role, lock acquire success/409/badge |
| `DisputeStatusModal.test.jsx` | 5 â€” Countdown timer, releaseLock on cancel, updateDispute on submit, expired lock, API error |
| `DisputeForm.test.jsx` | 4 â€” "Filing as", createDispute call, short-description validation, existing dispute disabled |
| `api.test.js` | 7 — Token header attached/missing, 401 clears sessionStorage, exports shape |

### Running Tests
```powershell
# Backend
cd backend/TransactionDisputePortal.Api.Tests
dotnet test

# Frontend
cd frontend/TransactionDisputePortal.Web
npm run test:run
```

---

## ðŸ—ï¸ Architecture

### Layered Architecture
```
Browser (React 19 + Vite)
  â†“ JWT Bearer token
ASP.NET Core 10 API
  â†“
Repository Layer (ITransactionRepository, IDisputeRepository, IUserRepository)
  â†“
Entity Framework Core 10 (Npgsql)
  â†“
PostgreSQL 16
```

### Design Patterns
- **Repository Pattern** â€” abstraction for data access
- **Dependency Injection** â€” loose coupling, testability
- **DTO Pattern** â€” `TransactionDto`, `DisputeDto` separate API contracts from entities
- **Soft-Lock Pattern** â€” three nullable columns on `Disputes` for pessimistic concurrency
- **Auth Context Pattern** â€” React context provides `user`, `token`, `login`, `logout` globally

---

## ðŸ“Š Technology Stack

| Layer | Technology | Version |
|---|---|---|
| **Frontend** | React | 19.2.6 |
| | Vite | 8.0.12 |
| | Axios | 1.7.2 |
| | Vitest | 4.1.9 |
| | React Testing Library | Latest |
| **Backend** | .NET | 10.0 |
| | ASP.NET Core | 10.0 |
| | Entity Framework Core | 10.0.9 |
| | Npgsql EF Provider | 10.0.2 |
| | JwtBearer | 10.0.0 |
| | Swashbuckle | 10.2.1 |
| | xUnit | 2.9.3 |
| | Moq | 4.20.72 |
| **Database** | PostgreSQL | 16 |
| **Deployment** | Docker + Docker Compose | Latest |

---

## ðŸ“ˆ Project Statistics

- **Backend test classes**: 7 (69 tests total)
- **Frontend test files**: 7 (41 tests total)
- **Backend controllers**: 4 (Auth, Transactions, Disputes, Health)
- **Frontend components**: 6 (LoginPage, App, TransactionsList, DisputeForm, DisputeHistory, DisputeStatusModal)
- **API endpoints**: 14
- **Database entities**: 3 (ApplicationUser, Transaction, Dispute)
- **EF migrations**: 1 (`AddUsersAndDisputeLocking`)

---

## ðŸ” Security Features

- âœ… JWT Bearer authentication (HS256, 24-hour expiry)
- âœ… Role-based authorization on every sensitive endpoint
- âœ… Password hashing via ASP.NET Identity `PasswordHasher<T>` (PBKDF2)
- âœ… HTTPS metadata required in production (`RequireHttpsMetadata = true`)
- âœ… Input validation on all API endpoints
- âœ… CORS configuration
- âœ… SQL injection prevention (EF Core parameterized queries)
- âœ… XSS prevention (React auto-escaping)
✅ `sessionStorage` used for token storage (cleared on tab/browser close; not accessible cross-tab)
- ✅ 5-minute inactivity timeout forces re-authentication on idle sessions
- - âœ… 401 response interceptor clears client-side session automatically
- âœ… Error messages without sensitive data exposure

---

## ðŸŽ¯ Production Readiness Checklist

- [x] Full-stack application built and tested
- [x] JWT authentication implemented
- [x] Role-based authorization on all endpoints
- [x] Dispute concurrency control (soft-locking)
- [x] Docker containerization complete
- [x] EF Core migration created
- [x] 69 backend unit tests — 0 failures
- [x] 41 frontend component tests — 0 failures
- [x] Comprehensive documentation
- [x] Seed data for all user roles
- [x] Security best practices applied
- [x] sessionStorage for auth tokens (replaces localStorage)
- [x] 5-minute inactivity auto-logout
- [x] Dispute status visible in transactions list
- [x] Dispute detail view for clients (status card + hidden form on duplicate)