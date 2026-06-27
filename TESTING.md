# Testing Guide - Capitec Transaction Dispute Portal

## Overview

This guide covers the actual test setup and strategies for the Capitec Transaction Dispute Portal.
**Current status: 69 backend tests + 41 frontend tests — 0 failures.**

## Table of Contents

1. [Running Tests](#running-tests)
2. [Backend Tests](#backend-tests)
3. [Frontend Tests](#frontend-tests)
4. [API Testing](#api-testing)
5. [Security Testing](#security-testing)
6. [CI/CD](#cicd)

---

## Running Tests

### Backend (xUnit)
```powershell
cd backend/TransactionDisputePortal.Api.Tests
dotnet test
```

### Frontend (Vitest)
```powershell
cd frontend/TransactionDisputePortal.Web
npm run test:run        # single run
npm run test            # watch mode
```

---

## Backend Tests

### Stack
| Package | Version | Purpose |
|---|---|---|
| xUnit | 2.9.3 | Test framework |
| Moq | 4.20.72 | Mocking |
| EF Core InMemory | 10.0.9 | In-memory DB for repositories |
| Microsoft.Extensions.Configuration | (shared) | IConfiguration mock support |

### Key Pattern — Fresh InMemory Context

Each test creates an isolated DB to avoid seed-data conflicts:

```csharp
private static ApplicationDbContext CreateFreshContext()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    return new ApplicationDbContext(options);
}
```

> ⚠️ Do **not** call `EnsureCreated()` — it runs `OnModelCreating` seed data which
> conflicts with explicit IDs used in tests.

### Repository Tests (21 tests)

#### UserRepositoryTests
```csharp
[Fact]
public async Task GetByUsername_ReturnsNull_WhenInactive()
{
    await using var ctx = CreateFreshContext();
    ctx.Users.Add(new ApplicationUser { Id = 1, Username = "alice",
        PasswordHash = "x", FullName = "Alice", Role = "Client", IsActive = false });
    await ctx.SaveChangesAsync();

    var repo   = new UserRepository(ctx);
    var result = await repo.GetByUsernameAsync("alice");

    Assert.Null(result);
}
```

#### DisputeRepositoryTests
```csharp
[Fact]
public async Task UpdateLock_SetsLockFields()
{
    await using var ctx = CreateFreshContext();
    var (_, d) = await SeedOneAsync(ctx);      // seeds Tx + Dispute

    var repo = new DisputeRepository(ctx);
    var now  = DateTime.UtcNow;
    await repo.UpdateLockAsync(d.Id, 2, "Banker One", now);

    var updated = await ctx.Disputes.FindAsync(d.Id);
    Assert.Equal(2,            updated!.LockedByUserId);
    Assert.Equal("Banker One", updated.LockedByName);
    Assert.Equal(now,          updated.LockedAt);
}
```

### Controller Tests (45 tests)

#### ControllerTestHelper — JWT claims injection

```csharp
// Helpers/ControllerTestHelper.cs
public static void SetUser(ControllerBase controller,
                           string role, int userId, string fullName)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Role,              role),
        new Claim(ClaimTypes.NameIdentifier,    userId.ToString()),
        new Claim("FullName",                   fullName),
    };
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        }
    };
}
```

#### AuthControllerTests
```csharp
[Fact]
public async Task Login_ReturnsToken_ForAdminRole()
{
    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo.Setup(r => r.GetByUsernameAsync("admin"))
                .ReturnsAsync(new ApplicationUser { Id = 1, Username = "admin",
                    PasswordHash = _hasher.HashPassword(null!, "Admin123!"),
                    FullName = "Admin User", Role = "Admin", IsActive = true });

    var mockConfig = new Mock<IConfiguration>();
    mockConfig.Setup(c => c["Jwt:Key"])    .Returns((string?)"TestKeyForUnitTests-MustBe32Chars!!");
    mockConfig.Setup(c => c["Jwt:Issuer"]).Returns((string?)"TestIssuer");

    var controller = new AuthController(mockUserRepo.Object, _hasher, mockConfig.Object);
    var result     = await controller.Login(new LoginDto { Username = "admin", Password = "Admin123!" });

    var ok = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(ok.Value);
}
```

> **Moq + nullable refs**: `Returns("value")` fails with nullable reference warnings.
> Use `Returns((string?)"value")` explicit cast.

#### DisputesControllerTests — lock scenarios
```csharp
[Fact]
public async Task AcquireLock_Returns409_WhenActivelyLockedByOther()
{
    var lockedAt = DateTime.UtcNow;
    var dispute  = new DisputeDto { Id = 1, CustomerId = 4,
        IsLocked = true, LockedByName = "Banker One", LockedAt = lockedAt };

    _mockDisputeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MapToEntity(dispute));

    ControllerTestHelper.SetUser(_controller, "Banker", 2, "Banker Two");
    var result = await _controller.AcquireLock(1);

    Assert.IsType<ConflictObjectResult>(result);
}
```

---

## Frontend Tests

### Stack
| Package | Purpose |
|---|---|
| Vitest 4.1.9 | Test runner |
| @testing-library/react | Component rendering |
| @testing-library/user-event | User interactions |
| @testing-library/jest-dom | DOM matchers |
| jsdom | Browser environment |

### Setup

`vite.config.js`:
```js
test: {
  environment: 'jsdom',
  globals: true,
  setupFiles: ['./src/test/setup.js'],
}
```

`src/test/setup.js`:
```js
import '@testing-library/jest-dom';
```

### AuthContext + sessionStorage

```jsx
// src/components/__tests__/LoginPage.test.jsx
vi.mock('../../context/AuthContext', () => ({
  useAuth: () => ({ login: mockLogin, isAuthenticated: false }),
}));

it('calls login with trimmed credentials on submit', async () => {
  const user = userEvent.setup();
  render(<LoginPage />);

  await user.type(screen.getByPlaceholderText(/username/i), '  admin  ');
  await user.type(screen.getByPlaceholderText(/password/i), 'Admin123!');
  await user.click(screen.getByRole('button', { name: /sign in/i }));

  expect(mockLogin).toHaveBeenCalledWith('admin', 'Admin123!');
});
```

### Form interaction — userEvent vs fireEvent

> Use `userEvent.setup()` for form submissions and select interactions.
> `fireEvent.click` does **not** reliably trigger React `onSubmit` on controlled forms.

```jsx
it('calls createDispute on valid submit', async () => {
  const user = userEvent.setup();
  render(<DisputeForm transaction={mockTx} onDisputeCreated={vi.fn()} onCancel={vi.fn()} />);

  await user.selectOptions(screen.getByRole('combobox'), 'Unauthorized Transaction');
  await user.type(screen.getByRole('textbox'), 'This is a detailed description.');
  await user.click(screen.getByRole('button', { name: /create dispute/i }));

  expect(mockCreateDispute).toHaveBeenCalled();
});
```

### Axios interceptors — vi.hoisted

```js
// src/services/__tests__/api.test.js
const refs = vi.hoisted(() => ({
  requestFn: null, responseSuccessFn: null, responseErrorFn: null
}));

vi.mock('axios', () => ({
  default: {
    create: () => ({
      interceptors: {
        request:  { use: (fn)         => { refs.requestFn = fn; } },
        response: { use: (ok, errFn)  => { refs.responseSuccessFn = ok; refs.responseErrorFn = errFn; } },
      },
    }),
  },
}));

it('clears sessionStorage on 401', async () => {
  sessionStorage.setItem('tdp_token', 'tok');
  await refs.responseErrorFn({ response: { status: 401 } }).catch(() => {});
  expect(sessionStorage.getItem('tdp_token')).toBeNull();
});
```

---

## API Testing

Use `API_TESTING.http` (requires VS Code REST Client extension):

```http
### 1. Login
POST http://localhost:5115/api/auth/login
Content-Type: application/json

{ "username": "client", "password": "Client123!" }

### 2. Get transactions (paste token from step 1)
GET http://localhost:5115/api/transactions
Authorization: Bearer {{token}}

### 3. File dispute
POST http://localhost:5115/api/disputes
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "transactionId": 3,
  "reason": "Unauthorized Transaction",
  "description": "I did not authorise this utility payment."
}

### 4. Acquire lock (as banker)
POST http://localhost:5115/api/disputes/3/lock
Authorization: Bearer {{bankerToken}}

### 5. Update dispute status
PUT http://localhost:5115/api/disputes/3
Authorization: Bearer {{bankerToken}}
Content-Type: application/json

{ "status": 2, "resolutionNotes": "Verified and resolved." }
```

---

## Security Testing

### OWASP Checks Covered

| Check | Implementation |
|---|---|
| SQL Injection | EF Core parameterized queries |
| XSS | React auto-escaping |
| Broken Authentication | JWT HS256 + 24h expiry |
| Broken Access Control | Role policies per endpoint + client cross-access 403 |
| Security Misconfiguration | `RequireHttpsMetadata=true` in production |
| CSRF | SPA + JWT Bearer (not cookie-based) — not applicable |

### Authorization boundary tests (from `DisputesControllerTests`)

```csharp
[Fact] public async Task DeleteDispute_Returns403_WhenBankerTriesToDelete() { ... }
[Fact] public async Task GetDispute_Returns403_WhenClientAccessesOtherCustomer() { ... }
[Fact] public async Task CreateDispute_Returns400_WhenClientOwnsWrongTransaction() { ... }
```

---

## CI/CD

### GitHub Actions

```yaml
name: CI

on: [push, pull_request]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore backend/
      - run: dotnet build backend/ --no-restore
      - run: dotnet test backend/TransactionDisputePortal.Api.Tests --no-build --verbosity normal

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: npm ci
        working-directory: frontend/TransactionDisputePortal.Web
      - run: npm run test:run
        working-directory: frontend/TransactionDisputePortal.Web
```

---

## Test Execution Checklist

- [x] 69 backend unit tests — 0 failures
- [x] 41 frontend component tests — 0 failures
- [x] Auth scenarios (login success/fail, role access, JWT claims)
- [x] Repository CRUD (InMemory, no seed conflict)
- [x] Dispute soft-lock acquire / release / conflict / expiry
- [x] sessionStorage cleared on 401 and inactivity
- [x] Dispute status badge in transactions list
- [x] Dispute form hidden when active dispute exists
- [ ] E2E tests (Playwright) — future enhancement
- [ ] Load testing (k6) — future enhancement

---

Last Updated: June 2026