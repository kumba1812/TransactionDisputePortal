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
                TransactionId = "TXN20260604001",
                Amount = 1250.50m,
                Description = "ATM Withdrawal",
                TransactionDate = DateTime.UtcNow.AddDays(-10),
                Merchant = "FNB ATM - Sandton",
                Category = "ATM Withdrawal",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = 2,
                CustomerId = 1,
                TransactionId = "TXN20260609002",
                Amount = 899.99m,
                Description = "Monthly Insurance Premium",
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                Merchant = "Old Mutual Insurance",
                Category = "Insurance",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = 3,
                CustomerId = 1,
                TransactionId = "TXN20260612003",
                Amount = 450.00m,
                Description = "Utility Payment",
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                Merchant = "Eskom - Electricity",
                Category = "Utilities",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = 4,
                CustomerId = 1,
                TransactionId = "TXN20260520004",
                Amount = 2000.00m,
                Description = "International Wire Transfer",
                TransactionDate = DateTime.UtcNow.AddDays(-25),
                Merchant = "Standard Chartered Bank - USA",
                Category = "Wire Transfer",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = 5,
                CustomerId = 1,
                TransactionId = "TXN20260614005",
                Amount = 325.50m,
                Description = "Card Purchase",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Merchant = "Pick n Pay - Westgate",
                Category = "Retail",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 6,
                CustomerId = 1,
                TransactionId = "TXN20260615006",
                Amount = 150.00m,
                Description = "Mobile Top-Up",
                TransactionDate = DateTime.UtcNow,
                Merchant = "Vodacom South Africa",
                Category = "Mobile Services",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
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
                Description = "I did not authorize this ATM withdrawal",
                Status = DisputeStatus.UnderReview,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                RefundAmount = 1250.50m
            },
            new()
            {
                Id = 2,
                TransactionId = 4,
                TransactionIdFk = 4,
                CustomerId = 1,
                Reason = "Incorrect Amount",
                Description = "Wire transfer was charged twice",
                Status = DisputeStatus.Resolved,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ResolvedAt = DateTime.UtcNow.AddDays(-15),
                ResolutionNotes = "Duplicate charge refunded to account",
                RefundAmount = 2000.00m
            }
        };

        modelBuilder.Entity<Dispute>().HasData(disputes);
    }
}
