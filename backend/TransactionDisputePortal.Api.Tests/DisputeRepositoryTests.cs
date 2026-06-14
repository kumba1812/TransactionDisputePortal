using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class DisputeRepositoryTests
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
    public async Task AddAndGetDispute()
    {
        var context = CreateContext();
        var txRepo = new TransactionRepository(context);
        var disputeRepo = new DisputeRepository(context);

        var tx = await txRepo.AddAsync(new Transaction { CustomerId = 5, TransactionId = "D-1", Amount = 20m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "desc" });
        var d = new Dispute { TransactionId = tx.Id, TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Fraud", Description = "desc" };

        var added = await disputeRepo.AddAsync(d);
        var fetched = await disputeRepo.GetByIdAsync(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Fraud", fetched!.Reason);
        Assert.Equal(tx.Id, fetched.TransactionId);
    }

    [Fact]
    public async Task GetByTransactionIdReturnsDisputes()
    {
        var context = CreateContext();
        var txRepo = new TransactionRepository(context);
        var disputeRepo = new DisputeRepository(context);

        var tx = await txRepo.AddAsync(new Transaction { CustomerId = 6, TransactionId = "D2-1", Amount = 5m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "desc" });

        await disputeRepo.AddAsync(new Dispute { TransactionId = tx.Id, TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Reason1", Description = "d1" });
        await disputeRepo.AddAsync(new Dispute { TransactionId = tx.Id, TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Reason2", Description = "d2" });

        var list = await disputeRepo.GetByTransactionIdAsync(tx.Id);
        Assert.Equal(2, list.Count());
    }
}
