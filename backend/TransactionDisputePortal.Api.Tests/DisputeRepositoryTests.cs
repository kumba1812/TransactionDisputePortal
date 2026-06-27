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

    private ApplicationDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(Transaction tx, Dispute dispute)> SeedOneAsync(
        ITransactionRepository txRepo, IDisputeRepository dRepo,
        int customerId = 10, string reason = "Fraud")
    {
        var tx = await txRepo.AddAsync(new Transaction
        {
            CustomerId = customerId,
            TransactionId = Guid.NewGuid().ToString("N")[..8],
            Amount = 20m,
            TransactionDate = DateTime.UtcNow,
            Merchant = "M",
            Category = "Cat",
            Description = "d"
        });
        var d = await dRepo.AddAsync(new Dispute
        {
            TransactionIdFk = tx.Id,
            CustomerId = tx.CustomerId,
            Reason = reason,
            Description = "desc",
            Status = DisputeStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        return (tx, d);
    }

    [Fact]
    public async Task AddAndGetDispute()
    {
        var context = CreateContext();
        var txRepo = new TransactionRepository(context);
        var disputeRepo = new DisputeRepository(context);

        var tx = await txRepo.AddAsync(new Transaction { CustomerId = 5, TransactionId = "D-1", Amount = 20m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "desc" });
        var d = new Dispute { TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Fraud", Description = "desc", CreatedAt = DateTime.UtcNow };

        var added = await disputeRepo.AddAsync(d);
        var fetched = await disputeRepo.GetByIdAsync(added.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Fraud", fetched!.Reason);
        Assert.Equal(tx.Id, fetched.TransactionIdFk);
    }

    [Fact]
    public async Task GetByTransactionIdReturnsDisputes()
    {
        var context = CreateContext();
        var txRepo = new TransactionRepository(context);
        var disputeRepo = new DisputeRepository(context);

        var tx = await txRepo.AddAsync(new Transaction { CustomerId = 6, TransactionId = "D2-1", Amount = 5m, TransactionDate = DateTime.UtcNow, Merchant = "M", Category = "Cat", Description = "desc" });

        await disputeRepo.AddAsync(new Dispute { TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Reason1", Description = "d1", CreatedAt = DateTime.UtcNow });
        await disputeRepo.AddAsync(new Dispute { TransactionIdFk = tx.Id, CustomerId = tx.CustomerId, Reason = "Reason2", Description = "d2", CreatedAt = DateTime.UtcNow });

        var list = await disputeRepo.GetByTransactionIdAsync(tx.Id);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task GetAll_ReturnsAllDisputes()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        await SeedOneAsync(txRepo, repo, customerId: 11, reason: "R1");
        await SeedOneAsync(txRepo, repo, customerId: 12, reason: "R2");

        var all = await repo.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task GetByCustomerId_FiltersCorrectly()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        await SeedOneAsync(txRepo, repo, customerId: 20, reason: "A");
        await SeedOneAsync(txRepo, repo, customerId: 20, reason: "B");
        await SeedOneAsync(txRepo, repo, customerId: 21, reason: "C");

        var cust20 = await repo.GetByCustomerIdAsync(20);
        Assert.Equal(2, cust20.Count());

        var cust21 = await repo.GetByCustomerIdAsync(21);
        Assert.Single(cust21);
    }

    [Fact]
    public async Task UpdateDispute_PersistsStatusChange()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        var (_, dispute) = await SeedOneAsync(txRepo, repo);
        dispute.Status = DisputeStatus.UnderReview;
        await repo.UpdateAsync(dispute);

        var fetched = await repo.GetByIdAsync(dispute.Id);
        Assert.Equal(DisputeStatus.UnderReview, fetched!.Status);
    }

    [Fact]
    public async Task DeleteDispute_RemovesFromDb()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        var (_, dispute) = await SeedOneAsync(txRepo, repo);
        await repo.DeleteAsync(dispute.Id);

        var fetched = await repo.GetByIdAsync(dispute.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task UpdateLock_SetsLockFields()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        var (_, dispute) = await SeedOneAsync(txRepo, repo);
        var lockTime = DateTime.UtcNow;

        await repo.UpdateLockAsync(dispute.Id, 42, "Banker One", lockTime);

        var fetched = await repo.GetByIdAsync(dispute.Id);
        Assert.Equal(42, fetched!.LockedByUserId);
        Assert.Equal("Banker One", fetched.LockedByName);
        Assert.NotNull(fetched.LockedAt);
    }

    [Fact]
    public async Task UpdateLock_ClearsLockFields()
    {
        var ctx = CreateFreshContext();
        var txRepo = new TransactionRepository(ctx);
        var repo = new DisputeRepository(ctx);

        var (_, dispute) = await SeedOneAsync(txRepo, repo);
        await repo.UpdateLockAsync(dispute.Id, 42, "Banker One", DateTime.UtcNow);

        // Now clear it
        await repo.UpdateLockAsync(dispute.Id, null, null, null);

        var fetched = await repo.GetByIdAsync(dispute.Id);
        Assert.Null(fetched!.LockedByUserId);
        Assert.Null(fetched.LockedByName);
        Assert.Null(fetched.LockedAt);
    }
}
