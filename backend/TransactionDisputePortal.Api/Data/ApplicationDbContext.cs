using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Dispute> Disputes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Merchant).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.HasMany(e => e.Disputes)
                .WithOne(d => d.Transaction)
                .HasForeignKey(d => d.TransactionIdFk);
        });

        // Dispute configuration
        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
        });

        // Seed sample data
        SeedSampleData(modelBuilder);
    }

    private void SeedSampleData(ModelBuilder modelBuilder)
    {
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                CustomerId = 1,
                TransactionId = "TXN001",
                Amount = 125.50m,
                Description = "Online Purchase",
                TransactionDate = DateTime.UtcNow.AddDays(-10),
                Merchant = "Amazon",
                Category = "Shopping",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = 2,
                CustomerId = 1,
                TransactionId = "TXN002",
                Amount = 89.99m,
                Description = "Electronics",
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                Merchant = "Best Buy",
                Category = "Electronics",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = 3,
                CustomerId = 1,
                TransactionId = "TXN003",
                Amount = 45.00m,
                Description = "Restaurant",
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                Merchant = "Pizza Hut",
                Category = "Dining",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = 4,
                CustomerId = 1,
                TransactionId = "TXN004",
                Amount = 200.00m,
                Description = "Flight Booking",
                TransactionDate = DateTime.UtcNow.AddDays(-15),
                Merchant = "Delta Airlines",
                Category = "Travel",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            }
        };

        modelBuilder.Entity<Transaction>().HasData(transactions);

        var disputes = new List<Dispute>
        {
            new()
            {
                Id = 1,
                TransactionId = 1,
                TransactionIdFk = 1,
                CustomerId = 1,
                Reason = "Unauthorized",
                Description = "I did not authorize this purchase",
                Status = DisputeStatus.UnderReview,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                RefundAmount = 125.50m
            },
            new()
            {
                Id = 2,
                TransactionId = 4,
                TransactionIdFk = 4,
                CustomerId = 1,
                Reason = "Duplicate Charge",
                Description = "This flight was charged twice",
                Status = DisputeStatus.Resolved,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ResolvedAt = DateTime.UtcNow.AddDays(-15),
                ResolutionNotes = "Refund processed",
                RefundAmount = 200.00m
            }
        };

        modelBuilder.Entity<Dispute>().HasData(disputes);
    }
}
