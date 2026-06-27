# Plan: Comprehensive Unit & Component Tests

## Summary
Add full test coverage for every CRUD and auth scenario. Backend uses xUnit + Moq + EF Core InMemory (already set up). Frontend needs `@testing-library/react` + `jsdom` added to Vitest. Both test suites run to zero failures.

## Current State
- Backend: xUnit + Moq + InMemory DB wired up. Only 3 basic repository tests exist. No controller tests, no auth tests, no lock tests.
- Frontend: Vitest installed, only a single sanity check `example.test.jsx`. No React Testing Library, no jsdom.
- `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom` are NOT installed.
- `TransactionDisputePortal.Api.Tests.csproj` has a typo: `</ProjectGroup>` should be `</ItemGroup>`.

## Test Scope

### Backend — what to cover
| Test class | Scenarios |
|---|---|
| `UserRepositoryTests` | Add, GetByUsername (found/not found/inactive), GetById (found/missing) |
| `TransactionRepositoryTests` | Add, GetById (found/not found), GetByCustomerId, GetAll, Update, Delete |
| `DisputeRepositoryTests` | Add, GetById, GetByCustomerId, GetAll, GetByTransactionId, Update, Delete, UpdateLock (set/clear) |
| `AuthControllerTests` | Login success for each role, wrong password → 401, missing fields → 400, inactive user → 401 |
| `TransactionsControllerTests` | GetTransactions (Admin=all, Client=own, Banker=all, ReadOnly=all), GetTransaction (found, not found, Client cross-access → 403), POST/PUT/DELETE (Admin=2xx, Client=403, Banker=403), DELETE 404 |
| `DisputesControllerTests` | GetDisputes (Banker=all, Client=own), GetDispute (found, 404, Client cross → 403), CreateDispute (Client=201, Banker=403, duplicate=400, wrong-owner=400), UpdateDispute (active lock=200, no lock=409, expired=409, Resolved sets ResolvedAt), DeleteDispute (Client own=204, Client cross=403, Banker=403, Admin=204), AcquireLock (unlocked=200, active lock by other=409, expired overwrite=200, own refresh=200), ReleaseLock (own=204, other→403) |
| `DisputeDtoTests` | IsLocked=true when LockedAt within expiry, IsLocked=false when LockedAt expired, IsLocked=false when null |

### Frontend — what to cover
| Test file | Scenarios |
|---|---|
| `LoginPage.test.jsx` | Renders username/password form, submit with valid data calls `authContext.login`, error message shown on rejection, loading state during submit |
| `App.test.jsx` | Shows LoginPage when `isAuthenticated=false`, shows main layout when `isAuthenticated=true`, role badge renders, logout button calls `authContext.logout` |
| `TransactionsList.test.jsx` | Renders transaction rows from API data, "View/Dispute" button present for Client, hidden for Banker, hidden for ReadOnly |
| `DisputeHistory.test.jsx` | Renders dispute cards, "Update Status" visible for Banker, hidden for Client, hidden for ReadOnly, lock badge shown when `isLocked=true` and `lockedByName !== currentUser`, inline warning shown on 409, modal opens on 200 |
| `DisputeStatusModal.test.jsx` | Renders countdown timer, calls `releaseLock` on cancel, calls `updateDispute` on submit, submit disabled when lock expired, error displayed on failure |
| `DisputeForm.test.jsx` | Shows "Filing as: [fullName]", submit calls `createDispute`, validation error on short description, form disabled when existing dispute present |
| `api.test.js` | Bearer token attached when token in localStorage, no header when absent, 401 response clears localStorage |

---

## Phase 1: Backend — Repository Tests (expand existing, parallel with Phase 2)

All use EF Core InMemory with a unique DB name per test (`Guid.NewGuid().ToString()`). Do NOT call `EnsureCreated()` — it runs `OnModelCreating` seed data that conflicts with tests inserting explicit IDs.

**`UserRepositoryTests.cs`** — new file:
1. `AddUser_Stores_And_ReturnsUser`
2. `GetByUsername_ReturnsActiveUser`
3. `GetByUsername_ReturnsNull_WhenNotFound`
4. `GetByUsername_ReturnsNull_WhenInactive` — set `IsActive = false`
5. `GetById_ReturnsUser`
6. `GetById_ReturnsNull_WhenMissing`

**`TransactionRepositoryTests.cs`** — expand existing 2 tests:
7. `UpdateTransaction_PersistsChanges` — add tx, update Description, fetch and assert
8. `DeleteTransaction_RemovesFromDb` — add tx, delete by id, GetById → null
9. `GetAll_ReturnsAllTransactions` — add 3 txns with different CustomerIds, GetAll returns 3

**`DisputeRepositoryTests.cs`** — expand existing 2 tests:
10. `UpdateDispute_PersistsStatusChange`
11. `DeleteDispute_RemovesFromDb`
12. `GetAll_ReturnsAllDisputes`
13. `GetByCustomerId_FiltersCorrectly`
14. `UpdateLock_SetsLockFields` — call `UpdateLockAsync(id, userId, name, now)`, fetch, assert all 3 fields set
15. `UpdateLock_ClearsLockFields` — set lock, then `UpdateLockAsync(id, null, null, null)`, assert all 3 null

---

## Phase 2: Backend — Controller Tests (new, parallel with Phase 1)

Use **Moq** to mock all repositories and `IConfiguration`. Inject a `ClaimsPrincipal` into `ControllerBase.ControllerContext` to simulate JWT auth without running the full middleware stack.

Add `Helpers/ControllerTestHelper.cs` — static builder: `BuildController<T>(T controller, string role, int userId, string fullName)`.

**`AuthControllerTests.cs`** — new file:
16. `Login_ValidAdmin_Returns200WithRole`
17. `Login_ValidBanker_Returns200WithRole`
18. `Login_ValidClient_Returns200WithRole`
19. `Login_WrongPassword_Returns401`
20. `Login_UnknownUsername_Returns401` — repo returns null
21. `Login_EmptyUsername_Returns400`
22. `Login_EmptyPassword_Returns400`

**`TransactionsControllerTests.cs`** — new file:
23. `GetTransactions_AsAdmin_ReturnsAll` — mock `GetAllAsync()` returns 3
24. `GetTransactions_AsClient_ReturnsOwn` — mock `GetByCustomerIdAsync(userId)` returns 2
25. `GetTransactions_AsBanker_ReturnsAll`
26. `GetTransactions_AsReadOnly_ReturnsAll`
27. `GetTransaction_Found_Returns200`
28. `GetTransaction_NotFound_Returns404`
29. `GetTransaction_AsClient_CrossAccess_Returns403` — fetched tx has different CustomerId
30. `CreateTransaction_AsAdmin_Returns201`
31. `UpdateTransaction_AsAdmin_Returns200`
32. `UpdateTransaction_NotFound_Returns404`
33. `DeleteTransaction_AsAdmin_Returns204`
34. `DeleteTransaction_NotFound_Returns404`

**`DisputesControllerTests.cs`** — new file:
35. `GetDisputes_AsBanker_ReturnsAll`
36. `GetDisputes_AsClient_ReturnsOwn`
37. `GetDispute_Found_Returns200`
38. `GetDispute_NotFound_Returns404`
39. `GetDispute_AsClient_CrossAccess_Returns403`
40. `GetDisputesByTransaction_Returns200`
41. `CreateDispute_AsClient_Returns201`
42. `CreateDispute_DuplicateTransaction_Returns400`
43. `CreateDispute_TransactionBelongsToOtherCustomer_Returns400`
44. `UpdateDispute_WithActiveLock_Returns200` — `LockedByUserId=currentUser`, `LockedAt=DateTime.UtcNow`
45. `UpdateDispute_WithNoLock_Returns409`
46. `UpdateDispute_WithExpiredLock_Returns409` — `LockedAt = DateTime.UtcNow.AddMinutes(-15)`
47. `UpdateDispute_ResolvedStatus_SetsResolvedAt`
48. `UpdateDispute_ClearsLockAfterSuccess`
49. `DeleteDispute_AsClient_OwnDispute_Returns204`
50. `DeleteDispute_AsClient_OtherDispute_Returns403`
51. `DeleteDispute_AsBanker_Returns403`
52. `DeleteDispute_AsAdmin_Returns204`
53. `DeleteDispute_NotFound_Returns404`
54. `AcquireLock_Unlocked_Returns200WithLockSet`
55. `AcquireLock_LockedByAnotherActive_Returns409WithLockerName`
56. `AcquireLock_LockedByExpiredLock_Returns200` — `LockedAt = 15 min ago` → overwritten
57. `AcquireLock_OwnLock_Returns200` — re-acquiring own lock refreshes it
58. `ReleaseLock_OwnLock_Returns204`
59. `ReleaseLock_OtherLock_Returns403`
60. `ReleaseLock_NotFound_Returns404`

**`DisputeDtoTests.cs`** — new file:
61. `IsLocked_True_WhenLockedAtWithinExpiry`
62. `IsLocked_False_WhenLockedAtExpired` — `LockedAt = 11 min ago`
63. `IsLocked_False_WhenLockedAtNull`

---

## Phase 3: Frontend — Testing Infrastructure Setup (blocks Phase 4)

64. Fix `</ProjectGroup>` → `</ItemGroup>` typo in `TransactionDisputePortal.Api.Tests.csproj`
65. Install test dependencies in `frontend/TransactionDisputePortal.Web/`:
    ```
    npm install -D @testing-library/react @testing-library/user-event @testing-library/jest-dom jsdom
    ```
66. Update `vite.config.js` — add:
    ```js
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.js'],
    }
    ```
67. Create `src/test/setup.js` — `import '@testing-library/jest-dom'`
68. Create `src/test/mocks/authContext.mock.js` — exports `mockUseAuth(role, overrides)` factory; `vi.mock('../../context/AuthContext')` convenience helper
69. Create `src/test/mocks/api.mock.js` — `vi.mock('../../services/api')` stubs for `transactionApi`, `disputeApi` returning resolved fixture data
70. Create `src/test/fixtures.js` — shared transaction and dispute fixture arrays used across all tests

---

## Phase 4: Frontend — Component Tests (depends on Phase 3)

Each test file lives in `src/components/__tests__/` (or `src/services/__tests__/`).

**`LoginPage.test.jsx`:**
71. Renders username input, password input, submit button
72. Calls `authContext.login(username, password)` on submit
73. Displays error message when `login()` rejects with message
74. Submit button shows "Signing in…" and is disabled while loading

**`App.test.jsx`:**
75. Renders `<LoginPage>` when `isAuthenticated=false`
76. Renders nav tabs when `isAuthenticated=true`
77. Header shows `user.fullName` and `user.role` badge text
78. Logout button calls `authContext.logout()`

**`TransactionsList.test.jsx`:**
79. Renders one table row per transaction from fixture
80. "View/Dispute" button present when `user.role === 'Client'`
81. "View/Dispute" button absent when `user.role === 'Banker'`
82. "View/Dispute" button absent when `user.role === 'ReadOnly'`
83. Clicking "View/Dispute" calls `onSelectTransaction(transaction)`

**`DisputeHistory.test.jsx`:**
84. Renders one card per dispute from fixture
85. "Update Status" button present for Banker
86. "Update Status" button absent for Client
87. "Update Status" button absent for ReadOnly
88. Lock badge `🔒 Banker One` rendered when `isLocked=true` and `lockedByName !== user.fullName`
89. Lock badge absent when `isLocked=false`
90. Inline warning rendered when `acquireLock` rejects with 409
91. `DisputeStatusModal` rendered when `acquireLock` resolves successfully

**`DisputeStatusModal.test.jsx`:**
92. Renders countdown timer showing initial time remaining
93. Clicking Cancel calls `disputeApi.releaseLock` and calls `onClose`
94. Submit calls `disputeApi.updateDispute` with status and notes
95. Submit button disabled when lock has expired (`secondsLeft === 0`)
96. Error message rendered when `updateDispute` rejects

**`DisputeForm.test.jsx`:**
97. Renders "Filing as: Client User" in form header
98. Submit calls `disputeApi.createDispute` with `{ transactionId, reason, description }`
99. Error shown when description is fewer than 10 characters
100. Form inputs and submit disabled when existing dispute present

**`api.test.js`:**
101. Request config includes `Authorization: Bearer abc123` when token in localStorage
102. No `Authorization` header when localStorage is empty
103. 401 response removes `tdp_token` and `tdp_user` from localStorage

---

## Phase 5: Run Tests + Fix Failures

104. Fix `.csproj` typo so `dotnet restore` succeeds
105. Run: `cd backend && dotnet test --verbosity normal`
106. Run: `cd frontend/TransactionDisputePortal.Web && npm run test:run`
107. Address any InMemory DB seed-data conflicts (use raw context without `EnsureCreated`)
108. Address any Vitest `vi.mock` hoisting issues (move mocks to top of file)
109. All tests green, zero skipped

---

## Key Files

**New/modified backend test files:**
- `backend/TransactionDisputePortal.Api.Tests/TransactionDisputePortal.Api.Tests.csproj` — fix `</ProjectGroup>` typo
- `backend/TransactionDisputePortal.Api.Tests/Helpers/ControllerTestHelper.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/UserRepositoryTests.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/AuthControllerTests.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/TransactionsControllerTests.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/DisputesControllerTests.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/DisputeDtoTests.cs` — **NEW**
- `backend/TransactionDisputePortal.Api.Tests/TransactionRepositoryTests.cs` — expand (add Update, Delete, GetAll)
- `backend/TransactionDisputePortal.Api.Tests/DisputeRepositoryTests.cs` — expand (add GetAll, GetByCustomerId, Update, Delete, UpdateLock x2)

**New/modified frontend files:**
- `frontend/TransactionDisputePortal.Web/package.json` — add 4 test devDependencies
- `frontend/TransactionDisputePortal.Web/vite.config.js` — add test block
- `frontend/TransactionDisputePortal.Web/src/test/setup.js` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/test/fixtures.js` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/test/mocks/authContext.mock.js` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/test/mocks/api.mock.js` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/LoginPage.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/App.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/TransactionsList.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/DisputeHistory.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/DisputeStatusModal.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/components/__tests__/DisputeForm.test.jsx` — **NEW**
- `frontend/TransactionDisputePortal.Web/src/services/__tests__/api.test.js` — **NEW**

---

## Verification

1. `cd backend && dotnet test --verbosity normal` → **0 failed, 0 skipped**
2. `cd frontend/TransactionDisputePortal.Web && npm run test:run` → **0 failed, 0 skipped**
3. Confirm `UpdateDispute_WithExpiredLock_Returns409` catches a 15-min-old `LockedAt` correctly
4. Confirm `AcquireLock_LockedByExpiredLock_Returns200` overwrites the stale lock
5. Confirm `DisputeHistory.test` — lock warning shown after mock 409, modal blocked
6. Confirm `TransactionsList.test` — "View/Dispute" absent in rendered DOM for Banker role

---

## Decisions
- **Controller tests use Moq + ClaimsPrincipal injection, not WebApplicationFactory** — fast and isolated; auth middleware is not exercised. Role enforcement on `[Authorize(Roles)]` attributes is tested by confirming 403/Forbid() is returned when a wrong-role principal is injected.
- **No `EnsureCreated()` in repository test helpers** — prevents `OnModelCreating` seed data from applying and colliding with test-inserted rows that share fixed IDs.
- **Frontend mocks `AuthContext` and `api.js` completely** — components are tested in isolation from network calls; Vitest's `vi.mock()` with module factory pattern is used.
- **Fixtures shared via `src/test/fixtures.js`** — avoids duplicate inline fixture objects across test files.
- **`</ProjectGroup>` typo fix is prerequisite** — `dotnet restore` fails with a malformed `.csproj`; must be fixed before any test run.
