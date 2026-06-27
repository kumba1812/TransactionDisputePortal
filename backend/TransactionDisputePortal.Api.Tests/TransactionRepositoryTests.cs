using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class TransactionRepositoryTests
{
    // With seed data (used by existing tests)
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    // Without seed data – use when verifying counts or full-table results
    private ApplicationDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Transaction SampleTx(int customerId = 1, string uid = "T-001") => new()
    {
        CustomerId = customerId,
        TransactionId = uid,
        Amount = 10.5m,
        Description = "Test",
        TransactionDate = DateTime.UtcNow,
        Merchant = "M",
        Category = "Cat"
    };

    [Fact]
    public async Task AddAndGetTransaction()
    {
        var context = CreateContext();
        var repo = new TransactionRepository(context);

        var tx = new Transaction
        {
            CustomerId = 1,
            TransactionId = "TST001",
            Amount = 10.5m,
            Description = "Test",
            TransactionDate = DateTime.UtcNow,
            Merchant = "UnitTest",
            Category = "Testing"
        };

        var added = await repo.AddAsync(tx);
        var fetched = await repo.GetByIdAsync(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal(added.TransactionId, fetched!.TransactionId);
        Assert.Equal(added.Amount, fetched.Amount);
    }

    [Fact]
    public async Task GetByCustomerReturnsTransactions()
    {
        var context = CreateContext();
        var repo = new TransactionRepository(context);

        await repo.AddAsync(new Transaction { CustomerId = 2, TransactionId = "C2-1", Amount = 1m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "d" });
        await repo.AddAsync(new Transaction { CustomerId = 2, TransactionId = "C2-2", Amount = 2m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "d" });

        var list = await repo.GetByCustomerIdAsync(2);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenMissing()
    {
        var repo = new TransactionRepository(CreateFreshContext());
        var result = await repo.GetByIdAsync(99999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllTransactions()
    {
        var context = CreateFreshContext();
        var repo = new TransactionRepository(context);

        await repo.AddAsync(SampleTx(1, "GA-1"));
        await repo.AddAsync(SampleTx(2, "GA-2"));
        await repo.AddAsync(SampleTx(3, "GA-3"));

        var all = await repo.GetAllAsync();
        Assert.Equal(3, all.Count());
    }

    [Fact]
    public async Task UpdateTransaction_PersistsChanges()
    {
        var context = CreateFreshContext();
        var repo = new TransactionRepository(context);

        var added = await repo.AddAsync(SampleTx(1, "UP-1"));
        added.Description = "Updated";
        await repo.UpdateAsync(added);

        var fetched = await repo.GetByIdAsync(added.Id);
        Assert.Equal("Updated", fetched!.Description);
    }

    [Fact]
    public async Task DeleteTransaction_RemovesFromDb()
    {
        var context = CreateFreshContext();
        var repo = new TransactionRepository(context);

        var added = await repo.AddAsync(SampleTx(1, "DEL-1"));
        await repo.DeleteAsync(added.Id);

        var fetched = await repo.GetByIdAsync(added.Id);
        Assert.Null(fetched);
    }
}
