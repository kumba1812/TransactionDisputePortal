# Testing Guide - Capitec Transaction Dispute Portal

## Overview

This guide covers testing strategies and procedures for the Capitec Transaction Dispute Portal.

## Table of Contents

1. [Unit Testing](#unit-testing)
2. [Integration Testing](#integration-testing)
3. [API Testing](#api-testing)
4. [Frontend Testing](#frontend-testing)
5. [Performance Testing](#performance-testing)
6. [Security Testing](#security-testing)

---

## Unit Testing

### Backend Unit Tests

Create test files in a test project:

```bash
mkdir backend/TransactionDisputePortal.Api.Tests
cd backend/TransactionDisputePortal.Api.Tests
dotnet new xunit
```

Update csproj:
```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.1" />
  <PackageReference Include="Moq" Version="4.20.69" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.9" />
</ItemGroup>
```

Example test:
```csharp
using Xunit;
using Moq;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Models;

public class DisputeRepositoryTests
{
	[Fact]
	public async Task GetByCustomerIdAsync_ReturnsDisputes_WhenFound()
	{
		// Arrange
		var mockContext = new Mock<ApplicationDbContext>();
		var repository = new DisputeRepository(mockContext.Object);

		// Act
		var result = await repository.GetByCustomerIdAsync(1);

		// Assert
		Assert.NotNull(result);
	}

	[Fact]
	public async Task CreateDispute_AddsDisputeToContext()
	{
		// Arrange
		var dispute = new Dispute 
		{ 
			CustomerId = 1, 
			Reason = "Unauthorized" 
		};

		// Act & Assert
		// Test implementation
	}
}
```

### Frontend Unit Tests

```bash
cd frontend/TransactionDisputePortal.Web
npm install --save-dev vitest @testing-library/react @testing-library/jest-dom
```

Example test:
```javascript
import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TransactionsList } from '../components/TransactionsList';

describe('TransactionsList', () => {
  it('renders loading state initially', () => {
	render(<TransactionsList onSelectTransaction={() => {}} />);
	expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('displays transactions when loaded', async () => {
	// Mock API response
	// Assert transactions are rendered
  });
});
```

Run frontend tests:
```bash
npm test
```

---

## Integration Testing

### Backend Integration Tests

```csharp
[Collection("Database collection")]
public class TransactionControllerIntegrationTests : IAsyncLifetime
{
	private readonly ApplicationDbContext _context;
	private readonly TransactionsController _controller;

	public async Task InitializeAsync()
	{
		// Setup test database
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		_context = new ApplicationDbContext(options);
		await _context.Database.EnsureCreatedAsync();
	}

	[Fact]
	public async Task GetTransactions_ReturnsAllTransactions()
	{
		// Arrange
		var transaction = new Transaction 
		{ 
			CustomerId = 1, 
			Amount = 100,
			Merchant = "Test Merchant"
		};
		_context.Transactions.Add(transaction);
		await _context.SaveChangesAsync();

		// Act
		var result = await _controller.GetTransactions();

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<Transaction>>(okResult.Value);
		Assert.Single(returnedTransactions);
	}

	public async Task DisposeAsync()
	{
		await _context.Database.EnsureDeletedAsync();
		_context.Dispose();
	}
}
```

---

## API Testing

### Manual Testing with HTTP Client

Use the `API_TESTING.http` file:

```http
### Get all transactions
GET http://localhost:5115/api/transactions

### Create dispute
POST http://localhost:5115/api/disputes
Content-Type: application/json

{
  "transactionId": 1,
  "reason": "Unauthorized",
  "description": "Test dispute"
}
```

### Automated API Testing with RestSharp

```csharp
[Fact]
public async Task CreateDispute_WithValidData_ReturnsCreated()
{
	// Arrange
	var client = new RestClient("http://localhost:5115");
	var request = new RestRequest("/api/disputes", Method.Post)
		.AddJsonBody(new { 
			transactionId = 1,
			reason = "Unauthorized",
			description = "Test"
		});

	// Act
	var response = await client.ExecuteAsync(request);

	// Assert
	Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
}
```

### Load Testing with k6

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export default function () {
  let response = http.get('http://localhost:5115/api/transactions');
  check(response, {
	'status is 200': (r) => r.status === 200,
	'response time < 500ms': (r) => r.timings.duration < 500,
  });
  sleep(1);
}
```

Run k6 test:
```bash
k6 run script.js
```

---

## Frontend Testing

### Component Testing

```javascript
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { DisputeForm } from '../components/DisputeForm';

describe('DisputeForm', () => {
  const mockTransaction = {
	id: 1,
	merchant: 'Amazon',
	amount: 100,
	transactionDate: new Date(),
	description: 'Test'
  };

  it('renders form with transaction details', () => {
	render(
	  <DisputeForm 
		transaction={mockTransaction}
		onDisputeCreated={() => {}}
		onCancel={() => {}}
	  />
	);

	expect(screen.getByText('Amazon')).toBeInTheDocument();
	expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('submits dispute when form is completed', async () => {
	const onDisputeCreated = vi.fn();
	render(
	  <DisputeForm 
		transaction={mockTransaction}
		onDisputeCreated={onDisputeCreated}
		onCancel={() => {}}
	  />
	);

	// Fill form
	fireEvent.change(screen.getByLabelText(/reason/i), {
	  target: { value: 'Unauthorized' }
	});
	fireEvent.change(screen.getByLabelText(/description/i), {
	  target: { value: 'Test dispute' }
	});

	// Submit
	fireEvent.click(screen.getByRole('button', { name: /create dispute/i }));

	// Assert
	await waitFor(() => {
	  expect(onDisputeCreated).toHaveBeenCalled();
	});
  });
});
```

### E2E Testing with Playwright

```bash
npm install --save-dev @playwright/test
npx playwright install
```

Create `e2e/transactions.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test('user can view transactions and create dispute', async ({ page }) => {
  // Navigate to app
  await page.goto('http://localhost:5173');

  // Check transactions are displayed
  await expect(page.locator('text=Recent Transactions')).toBeVisible();

  // Click on dispute button
  await page.click('button:has-text("View/Dispute")');

  // Fill dispute form
  await page.selectOption('select[name="reason"]', 'Unauthorized');
  await page.fill('textarea[name="description"]', 'I did not authorize this purchase');

  // Submit form
  await page.click('button:has-text("Create Dispute")');

  // Verify success
  await expect(page.locator('text=Dispute created successfully')).toBeVisible();
});
```

Run Playwright tests:
```bash
npx playwright test
```

---

## Performance Testing

### Backend Performance

```csharp
[Fact]
public async Task GetTransactions_WithLargeDataset_CompletesUnder500ms()
{
	// Arrange - seed large dataset
	var stopwatch = Stopwatch.StartNew();

	// Act
	var result = await _controller.GetTransactions();

	stopwatch.Stop();

	// Assert
	Assert.True(stopwatch.ElapsedMilliseconds < 500, 
		$"Request took {stopwatch.ElapsedMilliseconds}ms");
}
```

### Frontend Performance

```javascript
// Measure component render time
it('renders quickly with large dataset', () => {
  const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
	id: i,
	merchant: `Merchant ${i}`,
	amount: 100 + i,
  }));

  const start = performance.now();
  render(<TransactionsList transactions={largeDataset} />);
  const end = performance.now();

  expect(end - start).toBeLessThan(500);
});
```

---

## Security Testing

### OWASP Top 10 Checks

1. **SQL Injection**: Verify parameterized queries (✓ Using EF Core)
2. **XSS Prevention**: Check React auto-escaping
3. **CSRF Protection**: Implement CSRF tokens
4. **Authentication**: Test JWT validation
5. **Authorization**: Test role-based access

### Input Validation Tests

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public async Task CreateDispute_WithInvalidDescription_ReturnsBadRequest(string description)
{
	var request = new CreateDisputeRequest
	{
		TransactionId = 1,
		Reason = "Unauthorized",
		Description = description
	};

	var result = await _controller.CreateDispute(request);

	Assert.IsType<BadRequestObjectResult>(result);
}
```

---

## Continuous Integration

### GitHub Actions Example

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
	runs-on: ubuntu-latest

	steps:
	- uses: actions/checkout@v3

	- name: Setup .NET
	  uses: actions/setup-dotnet@v3
	  with:
		dotnet-version: '10.0.x'

	- name: Restore dependencies
	  run: dotnet restore backend/

	- name: Build
	  run: dotnet build backend/ --no-restore

	- name: Test
	  run: dotnet test backend/ --no-build --verbosity normal

	- name: Setup Node
	  uses: actions/setup-node@v3
	  with:
		node-version: '18'

	- name: Install frontend dependencies
	  run: npm ci
	  working-directory: frontend/TransactionDisputePortal.Web

	- name: Run frontend tests
	  run: npm test
	  working-directory: frontend/TransactionDisputePortal.Web
```

---

## Test Coverage

Target coverage metrics:
- Backend: > 80%
- Frontend: > 70%

Generate coverage reports:

**Backend:**
```bash
dotnet test backend/ /p:CollectCoverageMetrics=true
```

**Frontend:**
```bash
npm test -- --coverage
```

---

## Test Execution Checklist

- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] API tests pass
- [ ] E2E tests pass
- [ ] Performance benchmarks met
- [ ] Security tests pass
- [ ] Code coverage > target
- [ ] No console errors in frontend
- [ ] Database migrations work
- [ ] Docker build succeeds

---

Last Updated: January 2024
