# Plan: Auth + Role-Based Access Control

## Summary
Replace hardcoded demo credentials with DB-backed users, add 4 roles (Admin > Banker > Client > ReadOnly), enforce per-role access on controllers, and add a login page to the frontend.

## Current State
- AuthController uses hardcoded `demo/demo` and `admin/admin` (no DB)
- Dev middleware bypasses auth entirely (injects userId=1)
- No Users table in DB
- No password hashing
- Only one loose role check: `User.IsInRole("Admin")` in DisputesController
- Frontend has NO login page, no token handling, no protected routes
- api.js has no Authorization header logic

## Role Permission Matrix
| Endpoint | Admin | Banker | Client | ReadOnly |
|---|---|---|---|---|
| GET /transactions | All | All | Own | All |
| GET /transactions/{id} | Yes | Yes | Own | Yes |
| POST /transactions | Yes | No | No | No |
| PUT /transactions/{id} | Yes | No | No | No |
| DELETE /transactions/{id} | Yes | No | No | No |
| GET /disputes | All | All | Own | All |
| GET /disputes/{id} | Yes | Yes | Own | Yes |
| GET /disputes/transaction/{id} | Yes | Yes | Yes | Yes |
| POST /disputes | Yes | No | Own | No |
| PUT /disputes/{id} | Yes | Yes | No | No |
| DELETE /disputes/{id} | Yes | No | Own | No |

## Phase 1: Backend — User Model + DB (blocks Phase 2,3)
1. Add NuGet: `Microsoft.AspNetCore.Identity.Core` to .csproj (for PasswordHasher<T> only — no full Identity schema)
2. Create `Models/ApplicationUser.cs`: Id, Username, PasswordHash, FullName, Role (string), IsActive, CreatedAt
3. Add `DbSet<ApplicationUser> Users` to `ApplicationDbContext.cs`
4. Add `IUserRepository` and `UserRepository`: `GetByUsernameAsync(string)`, `GetByIdAsync(int)`, `AddAsync`, `UpdateAsync`
5. Register `IUserRepository` → `UserRepository` in Program.cs (scoped)
6. Create EF Core migration (`dotnet ef migrations add AddUsers`) — or handle via `EnsureCreated` with model update
7. Seed users in DbContext `OnModelCreating` (or migration): admin/Admin123!, banker/Banker123!, client/Client123!, readonly/Readonly123! (hashed at seed time using PasswordHasher)

## Phase 2: Backend — Auth Update (depends on Phase 1)
8. Rewrite `AuthController.Login`:
   - Inject `IUserRepository` and `IPasswordHasher<ApplicationUser>`
   - Look up user by username, verify with PasswordHasher
   - Build JWT with `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role`
   - Return token + role in `LoginResponse`
9. Register `IPasswordHasher<ApplicationUser>` in Program.cs
10. Remove dev bypass middleware from Program.cs (the anonymous identity injection block)
11. Add Swagger JWT SecurityDefinition + SecurityRequirement in Program.cs

## Phase 3: Backend — Authorization (depends on Phase 2)
12. Add authorization policies in Program.cs:
    - `"BankerOrAbove"`: roles Admin, Banker
    - `"ClientOrAbove"`: roles Admin, Client
    - `"WriteAccess"`: roles Admin only (for transaction mutations)
13. Add `ITransactionRepository.GetAllAsync()` and `IDisputeRepository.GetAllAsync()` to interfaces + implementations in Repository.cs
14. Update `TransactionsController`:
    - Add `[Authorize]` at class level
    - `GetTransactions`: if Client → GetByCustomerIdAsync(userId), else GetAllAsync()
    - `GetTransaction`: if Client → verify ownership
    - `POST/PUT/DELETE`: `[Authorize(Roles = "Admin")]` only
    - ReadOnly: blocked on POST/PUT/DELETE via role check
15. Update `DisputesController`:
    - Add `[Authorize]` at class level
    - `GetDisputes`: if Client → GetByCustomerIdAsync(userId), else GetAllAsync()
    - `GetDispute`: if Client → verify ownership
    - `POST CreateDispute`: `[Authorize(Roles = "Admin,Client")]` — Banker blocked
    - `PUT UpdateDispute`: `[Authorize(Roles = "Admin,Banker")]` — Client blocked
    - `DELETE`: `[Authorize(Roles = "Admin")]` or own (Client) — check inside method
    - `GetDisputesByTransaction`: accessible to all authenticated roles

## Phase 4: Frontend — Auth Infrastructure (parallel with Phase 3)
16. Create `src/context/AuthContext.jsx`: login(), logout(), user (id, username, role), token stored in localStorage
17. Create `src/components/LoginPage.jsx`: username + password form, calls POST /api/auth/login, stores token, redirects to app
18. Update `src/services/api.js`: read token from localStorage, add `Authorization: Bearer {token}` header to all requests; on 401 → clear token + redirect to login
19. Wrap App in `AuthProvider`, render `<LoginPage>` if not authenticated in `App.jsx`

## Phase 5: Frontend — Role-based UI (depends on Phase 4)
20. In `App.jsx`: show Dispute History tab for all roles (clients need to see their own disputes); hide Update Status button for clients
21. Hide "Dispute Transaction" button in `TransactionsList.jsx` for Banker and ReadOnly roles
22. In `DisputeHistory.jsx`: hide "Update Status" button for Client and ReadOnly roles
23. Add user info (username + role) + Logout button to header in `App.jsx`

## Phase 6: Seed SQL Update
24. Update `backend/docker/init/seed.sql` to add Users table DDL + INSERT for 4 seed users with pre-hashed passwords

## Key Files to Modify
- `backend/TransactionDisputePortal.Api/TransactionDisputePortal.Api.csproj` — add Identity.Core package
- `backend/TransactionDisputePortal.Api/Models/ApplicationUser.cs` — NEW
- `backend/TransactionDisputePortal.Api/Data/ApplicationDbContext.cs` — add Users DbSet + seed
- `backend/TransactionDisputePortal.Api/Repositories/IRepository.cs` — add IUserRepository + GetAllAsync to others
- `backend/TransactionDisputePortal.Api/Repositories/Repository.cs` — add UserRepository + GetAllAsync impls
- `backend/TransactionDisputePortal.Api/Controllers/AuthController.cs` — rewrite Login
- `backend/TransactionDisputePortal.Api/Controllers/TransactionsController.cs` — add [Authorize] + role checks
- `backend/TransactionDisputePortal.Api/Controllers/DisputesController.cs` — add [Authorize] + role checks
- `backend/TransactionDisputePortal.Api/Program.cs` — remove dev bypass, add policies, register PasswordHasher
- `frontend/TransactionDisputePortal.Web/src/services/api.js` — add auth header
- `frontend/TransactionDisputePortal.Web/src/App.jsx` — add auth guard + role-based tabs
- `frontend/TransactionDisputePortal.Web/src/context/AuthContext.jsx` — NEW
- `frontend/TransactionDisputePortal.Web/src/components/LoginPage.jsx` — NEW
- `frontend/TransactionDisputePortal.Web/src/components/TransactionsList.jsx` — hide dispute button by role
- `frontend/TransactionDisputePortal.Web/src/components/DisputeHistory.jsx` — hide update button by role
- `backend/docker/init/seed.sql` — add Users table + seed users

## Verification
1. Run backend: `dotnet run` — confirm no errors, `/api/health` returns 200
2. POST `/api/auth/login` with `{"username":"client","password":"Client123!"}` → 200 + JWT containing role=Client
3. POST `/api/auth/login` with wrong password → 401
4. GET `/api/transactions` with Client JWT → only own transactions
5. GET `/api/transactions` with Banker JWT → all transactions
6. POST `/api/disputes` with Banker JWT → 403 Forbidden
7. PUT `/api/disputes/{id}` with Client JWT → 403 Forbidden
8. GET `/api/disputes` with ReadOnly JWT → all disputes, no mutations allowed
9. Frontend: load app → redirected to login page
10. Login as client → Dispute History visible (own data only), "Dispute Transaction" button present, "Update Status" hidden
11. Login as banker → both tabs, no "Dispute Transaction" button, "Update Status" visible
12. Logout → token cleared, back to login page

## Decisions
- Using `Microsoft.AspNetCore.Identity.Core` for PasswordHasher only (no full Identity infra)
- Roles stored as plain strings in Users.Role column (not a separate roles table) — simple for 4 fixed roles
- Token stored in localStorage (consistent with existing SPA pattern)
- Seed users use pre-hashed passwords — DO NOT store plaintext in seed files
- `RequireHttpsMetadata` stays true in prod; set false in Development environment for local HTTP
- Dev bypass middleware REMOVED — login required even in development
