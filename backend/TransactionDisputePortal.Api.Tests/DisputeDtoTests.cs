using TransactionDisputePortal.Api.Dtos;
using TransactionDisputePortal.Api.Models;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class DisputeDtoTests
{
    private static Dispute MakeDispute(DateTime? lockedAt) => new()
    {
        Id = 1,
        TransactionIdFk = 1,
        CustomerId = 4,
        Reason = "Fraud",
        Description = "desc",
        Status = DisputeStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        LockedByUserId = lockedAt.HasValue ? 2 : null,
        LockedByName = lockedAt.HasValue ? "Banker One" : null,
        LockedAt = lockedAt,
        Transaction = new Transaction
        {
            Id = 1,
            CustomerId = 4,
            TransactionId = "TX-001",
            Amount = 100m,
            Description = "d",
            TransactionDate = DateTime.UtcNow,
            Merchant = "M",
            Category = "Cat"
        }
    };

    [Fact]
    public void IsLocked_True_WhenLockedAtWithinExpiry()
    {
        var dispute = MakeDispute(DateTime.UtcNow.AddMinutes(-5)); // locked 5 min ago, expiry=10 min
        var dto = new DisputeDto(dispute);

        Assert.True(dto.IsLocked);
        Assert.Equal("Banker One", dto.LockedByName);
    }

    [Fact]
    public void IsLocked_False_WhenLockedAtExpired()
    {
        var dispute = MakeDispute(DateTime.UtcNow.AddMinutes(-15)); // locked 15 min ago, expired
        var dto = new DisputeDto(dispute);

        Assert.False(dto.IsLocked);
    }

    [Fact]
    public void IsLocked_False_WhenLockedAtIsNull()
    {
        var dispute = MakeDispute(null);
        var dto = new DisputeDto(dispute);

        Assert.False(dto.IsLocked);
        Assert.Null(dto.LockedByName);
    }
}
