using Microsoft.AspNetCore.Mvc;
using Moq;
using TransactionDisputePortal.Api.Controllers;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Tests.Helpers;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class TransactionsControllerTests
{
    private static Transaction SampleTx(int id = 1, int customerId = 1) => new()
    {
        Id = id,
        CustomerId = customerId,
        TransactionId = $"TX-{id:000}",
        Amount = 50m,
        Description = "Test",
        TransactionDate = DateTime.UtcNow,
        Merchant = "Merchant",
        Category = "Food"
    };

    private static TransactionsController Build(out Mock<ITransactionRepository> repoMock, string role = "Admin", int userId = 1)
    {
        repoMock = new Mock<ITransactionRepository>();
        var ctrl = new TransactionsController(repoMock.Object);
        ControllerTestHelper.SetUser(ctrl, role, userId);
        return ctrl;
    }

    // ── GetTransactions ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_AsAdmin_ReturnsAll()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { SampleTx(1), SampleTx(2) });

        var result = await ctrl.GetTransactions();

        var ok = Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_AsBanker_ReturnsAll()
    {
        var ctrl = Build(out var repo, "Banker");
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { SampleTx(1) });

        await ctrl.GetTransactions();

        repo.Verify(r => r.GetAllAsync(), Times.Once);
        repo.Verify(r => r.GetByCustomerIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetTransactions_AsReadOnly_ReturnsAll()
    {
        var ctrl = Build(out var repo, "ReadOnly");
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { SampleTx(1) });

        await ctrl.GetTransactions();

        repo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_AsClient_ReturnsOnlyOwn()
    {
        var ctrl = Build(out var repo, "Client", userId: 4);
        repo.Setup(r => r.GetByCustomerIdAsync(4)).ReturnsAsync(new[] { SampleTx(1, 4) });

        await ctrl.GetTransactions();

        repo.Verify(r => r.GetByCustomerIdAsync(4), Times.Once);
        repo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    // ── GetTransaction ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransaction_ReturnsOk_WhenFound()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1, 1));

        var result = await ctrl.GetTransaction(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTransaction_ReturnsNotFound_WhenMissing()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);

        var result = await ctrl.GetTransaction(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetTransaction_AsClient_CrossAccess_ReturnsForbid()
    {
        var ctrl = Build(out var repo, "Client", userId: 4);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1, customerId: 99)); // belongs to user 99

        var result = await ctrl.GetTransaction(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetTransaction_AsClient_OwnTransaction_ReturnsOk()
    {
        var ctrl = Build(out var repo, "Client", userId: 4);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1, customerId: 4));

        var result = await ctrl.GetTransaction(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // ── CreateTransaction ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTransaction_AsAdmin_ReturnsCreated()
    {
        var ctrl = Build(out var repo, "Admin", userId: 1);
        var tx = SampleTx(10, 1);
        repo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(tx);

        var result = await ctrl.CreateTransaction(new CreateTransactionRequest
        {
            Amount = 100m,
            Description = "Purchase",
            TransactionDate = DateTime.UtcNow,
            Merchant = "Shop",
            Category = "Retail"
        });

        Assert.IsType<CreatedAtActionResult>(result);
        repo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
    }

    // ── UpdateTransaction ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTransaction_AsAdmin_ReturnsOk()
    {
        var ctrl = Build(out var repo, "Admin");
        var tx = SampleTx(1);
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tx);

        var result = await ctrl.UpdateTransaction(1, new UpdateTransactionRequest
        {
            Description = "Updated",
            Status = TransactionStatus.Completed
        });

        Assert.IsType<OkObjectResult>(result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTransaction_ReturnsNotFound_WhenMissing()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);

        var result = await ctrl.UpdateTransaction(99, new UpdateTransactionRequest { Description = "x" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── DeleteTransaction ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_AsAdmin_ReturnsNoContent()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleTx(1));

        var result = await ctrl.DeleteTransaction(1);

        Assert.IsType<NoContentResult>(result);
        repo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_ReturnsNotFound_WhenMissing()
    {
        var ctrl = Build(out var repo, "Admin");
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Transaction?)null);

        var result = await ctrl.DeleteTransaction(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
