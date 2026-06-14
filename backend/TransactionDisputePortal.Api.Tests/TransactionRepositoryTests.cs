using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class TransactionRepositoryTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

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
}
