using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TransactionDisputePortal.Api.Controllers;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Models.Dispute;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Tests.Helpers;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class DisputesControllerTests
{
    private const int BankerId = 2;
    private const int ClientId = 4;
    private const int AdminId = 1;

    private static IConfiguration BuildConfig(int lockMinutes = 10)
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Disputes:LockExpiryMinutes"]).Returns((string?)lockMinutes.ToString());
        return mock.Object;
    }

    private static DisputesController Build(
        out Mock<IDisputeRepository> disputeRepo,
        out Mock<ITransactionRepository> txRepo,
        string role = "Banker",
        int userId = BankerId)
    {
        disputeRepo = new Mock<IDisputeRepository>();
        txRepo = new Mock<ITransactionRepository>();
        var ctrl = new DisputesController(disputeRepo.Object, txRepo.Object, BuildConfig(), NullLogger<DisputesController>.Instance);
        ControllerTestHelper.SetUser(ctrl, role, userId, "Banker One");
        return ctrl;
    }

    private static TransactionEntity SampleTx(int id = 1, int customerId = ClientId) => new()
    {
        Id = id,
        CustomerId = customerId,
        TransactionId = $"TX-{id:000}",
        Amount = 100m,
        Description = "d",
        TransactionDate = DateTime.UtcNow,
        Merchant = "M",
        Category = "Cat"
    };

    private static DisputeEntity SampleDispute(int id = 1, int customerId = ClientId,
        int? lockedByUserId = null, string? lockedByName = null, DateTime? lockedAt = null) => new()
    {
        Id = id,
        TransactionIdFk = 1,
        CustomerId = customerId,
        Reason = "Fraud",
        Description = "desc",
        Status = DisputeStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        RefundAmount = 100m,
        LockedByUserId = lockedByUserId,
        LockedByName = lockedByName,
        LockedAt = lockedAt,
        Transaction = SampleTx()
    };

    // ── GetDisputes ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDisputes_AsBanker_ReturnsAll()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { SampleDispute(1), SampleDispute(2) });

        var result = await ctrl.GetDisputes();

        var ok = Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDisputes_AsClient_ReturnsOwn()
    {
        var ctrl = Build(out var repo, out _, "Client", ClientId);
        repo.Setup(r => r.GetByCustomerIdAsync(ClientId))
            .ReturnsAsync(new[] { SampleDispute(1, ClientId) });

        var result = await ctrl.GetDisputes();

        var ok = Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.GetByCustomerIdAsync(ClientId), Times.Once);
        repo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    // ── GetDispute ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDispute_ReturnsOk_WhenFound()
    {
        var ctrl = Build(out var repo, out _, "Admin", AdminId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1));

        var result = await ctrl.GetDispute(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDispute_ReturnsNotFound_WhenMissing()
    {
        var ctrl = Build(out var repo, out _, "Admin", AdminId);
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DisputeEntity?)null);

        var result = await ctrl.GetDispute(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDispute_AsClient_CrossAccess_ReturnsForbid()
    {
        var ctrl = Build(out var repo, out _, "Client", ClientId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1, customerId: 99));

        var result = await ctrl.GetDispute(1);

        Assert.IsType<ForbidResult>(result);
    }

    // ── CreateDispute ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDispute_AsClient_WithValidTransaction_Returns201()
    {
        var ctrl = Build(out var repo, out var txRepo, "Client", ClientId);
        var tx = SampleTx(1, ClientId);
        txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tx);
        repo.Setup(r => r.GetByTransactionIdAsync(1)).ReturnsAsync(Array.Empty<DisputeEntity>());
        repo.Setup(r => r.AddAsync(It.IsAny<DisputeEntity>())).ReturnsAsync(SampleDispute());

        var result = await ctrl.CreateDispute(new CreateDisputeRequest
        {
            TransactionId = 1,
            Reason = "Fraud",
            Description = "Detailed description"
        });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task CreateDispute_DuplicateDispute_Returns400()
    {
        var ctrl = Build(out var repo, out var txRepo, "Client", ClientId);
        txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1, ClientId));
        repo.Setup(r => r.GetByTransactionIdAsync(1)).ReturnsAsync(new[] { SampleDispute() });

        var result = await ctrl.CreateDispute(new CreateDisputeRequest
        {
            TransactionId = 1,
            Reason = "Fraud",
            Description = "Duplicate"
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDispute_WrongOwner_Returns400()
    {
        // Client 4 tries to dispute transaction owned by customer 99
        var ctrl = Build(out var repo, out var txRepo, "Client", ClientId);
        txRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1, customerId: 99));
        repo.Setup(r => r.GetByTransactionIdAsync(1)).ReturnsAsync(Array.Empty<DisputeEntity>());

        var result = await ctrl.CreateDispute(new CreateDisputeRequest
        {
            TransactionId = 1,
            Reason = "Fraud",
            Description = "Not my transaction"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── UpdateDispute ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDispute_WithActiveLock_Returns200()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1, lockedByUserId: BankerId, lockedAt: DateTime.UtcNow);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dispute);

        var result = await ctrl.UpdateDispute(1, new UpdateDisputeRequest
        {
            Status = DisputeStatus.UnderReview,
            ResolutionNotes = "Investigating"
        });

        Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<DisputeEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDispute_WithNoLock_Returns409()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1)); // no lock

        var result = await ctrl.UpdateDispute(1, new UpdateDisputeRequest
        {
            Status = DisputeStatus.UnderReview
        });

        Assert.IsType<ConflictObjectResult>(result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<DisputeEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDispute_WithExpiredLock_Returns409()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1,
            lockedByUserId: BankerId,
            lockedAt: DateTime.UtcNow.AddMinutes(-15)); // expired 15 min ago
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dispute);

        var result = await ctrl.UpdateDispute(1, new UpdateDisputeRequest
        {
            Status = DisputeStatus.UnderReview
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDispute_Resolved_SetsResolvedAt()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1, lockedByUserId: BankerId, lockedAt: DateTime.UtcNow);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dispute);
        DisputeEntity? savedDispute = null;
        repo.Setup(r => r.UpdateAsync(It.IsAny<DisputeEntity>()))
            .Callback<DisputeEntity>(d => savedDispute = d)
            .Returns(Task.CompletedTask);

        await ctrl.UpdateDispute(1, new UpdateDisputeRequest { Status = DisputeStatus.Resolved });

        Assert.NotNull(savedDispute!.ResolvedAt);
    }

    // ── DeleteDispute ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDispute_AsAdmin_Returns204()
    {
        var ctrl = Build(out var repo, out _, "Admin", AdminId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1));

        var result = await ctrl.DeleteDispute(1);

        Assert.IsType<NoContentResult>(result);
        repo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteDispute_AsClient_OwnDispute_Returns204()
    {
        var ctrl = Build(out var repo, out _, "Client", ClientId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1, customerId: ClientId));

        var result = await ctrl.DeleteDispute(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteDispute_AsClient_CrossAccess_ReturnsForbid()
    {
        var ctrl = Build(out var repo, out _, "Client", ClientId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1, customerId: 99));

        var result = await ctrl.DeleteDispute(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteDispute_AsBanker_ReturnsForbid()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        // Even when CustomerId matches, Banker cannot delete
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleDispute(1, customerId: BankerId));

        var result = await ctrl.DeleteDispute(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteDispute_Returns404_WhenMissing()
    {
        var ctrl = Build(out var repo, out _, "Admin", AdminId);
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DisputeEntity?)null);

        var result = await ctrl.DeleteDispute(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── AcquireLock ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AcquireLock_Unlocked_Returns200()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1); // no lock
        var lockedDispute = SampleDispute(1,
            lockedByUserId: BankerId, lockedByName: "Banker One", lockedAt: DateTime.UtcNow);

        repo.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(dispute)         // first call: check current lock state
            .ReturnsAsync(lockedDispute);  // second call: return updated data

        var result = await ctrl.AcquireLock(1);

        Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.UpdateLockAsync(1, BankerId, It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task AcquireLock_LockedByOther_ActiveLock_Returns409()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1,
            lockedByUserId: 99, lockedByName: "Other Banker",
            lockedAt: DateTime.UtcNow); // active lock by user 99
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dispute);

        var result = await ctrl.AcquireLock(1);

        Assert.IsType<ConflictObjectResult>(result);
        repo.Verify(r => r.UpdateLockAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public async Task AcquireLock_ExpiredLock_ByOther_Returns200()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1,
            lockedByUserId: 99, lockedByName: "Old Banker",
            lockedAt: DateTime.UtcNow.AddMinutes(-20)); // expired lock
        var lockedDispute = SampleDispute(1,
            lockedByUserId: BankerId, lockedAt: DateTime.UtcNow);

        repo.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(dispute)
            .ReturnsAsync(lockedDispute);

        var result = await ctrl.AcquireLock(1);

        Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.UpdateLockAsync(1, BankerId, It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task AcquireLock_OwnLock_Refresh_Returns200()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        var dispute = SampleDispute(1,
            lockedByUserId: BankerId, lockedByName: "Banker One",
            lockedAt: DateTime.UtcNow.AddMinutes(-5)); // own lock, still active
        var refreshedDispute = SampleDispute(1,
            lockedByUserId: BankerId, lockedAt: DateTime.UtcNow);

        repo.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(dispute)
            .ReturnsAsync(refreshedDispute);

        var result = await ctrl.AcquireLock(1);

        // Own lock is never "heldByAnother" → always succeeds
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AcquireLock_Returns404_WhenDisputeMissing()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DisputeEntity?)null);

        var result = await ctrl.AcquireLock(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── ReleaseLock ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReleaseLock_OwnLock_Returns204()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            SampleDispute(1, lockedByUserId: BankerId));

        var result = await ctrl.ReleaseLock(1);

        Assert.IsType<NoContentResult>(result);
        repo.Verify(r => r.UpdateLockAsync(1, null, null, null), Times.Once);
    }

    [Fact]
    public async Task ReleaseLock_OtherBankerLock_ReturnsForbid()
    {
        var ctrl = Build(out var repo, out _, "Banker", BankerId);
        // Lock held by user 99, current user is BankerId=2
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            SampleDispute(1, lockedByUserId: 99));

        var result = await ctrl.ReleaseLock(1);

        Assert.IsType<ForbidResult>(result);
        repo.Verify(r => r.UpdateLockAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public async Task ReleaseLock_Admin_CanReleaseAnyLock()
    {
        var ctrl = Build(out var repo, out _, "Admin", AdminId);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            SampleDispute(1, lockedByUserId: 99)); // locked by someone else

        var result = await ctrl.ReleaseLock(1);

        Assert.IsType<NoContentResult>(result);
        repo.Verify(r => r.UpdateLockAsync(1, null, null, null), Times.Once);
    }
}
