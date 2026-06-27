using Microsoft.AspNetCore.Identity;
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
    public DbSet<ApplicationUser> Users { get; set; }

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
            entity.Property(e => e.LockedByName).HasMaxLength(200);
        });

        // User configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
        });

        SeedSampleData(modelBuilder);
    }

    private void SeedSampleData(ModelBuilder modelBuilder)
    {
        // Seed users â€” passwords hashed with ASP.NET Identity PasswordHasher
        var hasher = new PasswordHasher<ApplicationUser>();
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var adminUser    = new ApplicationUser { Id = 1, Username = "admin",   FullName = "Admin User",    Role = "Admin",    IsActive = true, CreatedAt = seedDate };
        var bankerUser   = new ApplicationUser { Id = 2, Username = "banker",  FullName = "Banker One",    Role = "Banker",   IsActive = true, CreatedAt = seedDate };
        var banker2User  = new ApplicationUser { Id = 3, Username = "banker2", FullName = "Banker Two",    Role = "Banker",   IsActive = true, CreatedAt = seedDate };
        var clientUser   = new ApplicationUser { Id = 4, Username = "client",  FullName = "Client User",   Role = "Client",   IsActive = true, CreatedAt = seedDate };
        var readonlyUser = new ApplicationUser { Id = 5, Username = "readonly", FullName = "ReadOnly User", Role = "ReadOnly", IsActive = true, CreatedAt = seedDate };

        adminUser.PasswordHash    = hasher.HashPassword(adminUser,    "Admin123!");
        bankerUser.PasswordHash   = hasher.HashPassword(bankerUser,   "Banker123!");
        banker2User.PasswordHash  = hasher.HashPassword(banker2User,  "Banker2123!");
        clientUser.PasswordHash   = hasher.HashPassword(clientUser,   "Client123!");
        readonlyUser.PasswordHash = hasher.HashPassword(readonlyUser, "Readonly123!");

        modelBuilder.Entity<ApplicationUser>().HasData(adminUser, bankerUser, banker2User, clientUser, readonlyUser);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = 1,
                CustomerId = 4, // client user id
                TransactionId = "TXN20260604001",
                Amount = 1250.50m,
                Description = "ATM Withdrawal",
                TransactionDate = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "FNB ATM - Sandton",
                Category = "ATM Withdrawal",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                CustomerId = 4,
                TransactionId = "TXN20260609002",
                Amount = 899.99m,
                Description = "Monthly Insurance Premium",
                TransactionDate = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "Old Mutual Insurance",
                Category = "Insurance",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 3,
                CustomerId = 4,
                TransactionId = "TXN20260612003",
                Amount = 450.00m,
                Description = "Utility Payment",
                TransactionDate = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "Eskom - Electricity",
                Category = "Utilities",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 4,
                CustomerId = 4,
                TransactionId = "TXN20260520004",
                Amount = 2000.00m,
                Description = "International Wire Transfer",
                TransactionDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "Standard Chartered Bank - USA",
                Category = "Wire Transfer",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 5,
                CustomerId = 4,
                TransactionId = "TXN20260614005",
                Amount = 325.50m,
                Description = "Card Purchase",
                TransactionDate = new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "Pick n Pay - Westgate",
                Category = "Retail",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 6,
                CustomerId = 4,
                TransactionId = "TXN20260615006",
                Amount = 150.00m,
                Description = "Mobile Top-Up",
                TransactionDate = new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc),
                Merchant = "Vodacom South Africa",
                Category = "Mobile Services",
                Status = TransactionStatus.Completed,
                CreatedAt = new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc)
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
                CustomerId = 4,
                Reason = "Unauthorized",
                Description = "I did not authorize this ATM withdrawal",
                Status = DisputeStatus.UnderReview,
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc),
                RefundAmount = 1250.50m
            },
            new()
            {
                Id = 2,
                TransactionId = 4,
                TransactionIdFk = 4,
                CustomerId = 4,
                Reason = "Incorrect Amount",
                Description = "Wire transfer was charged twice",
                Status = DisputeStatus.Resolved,
                CreatedAt = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc),
                ResolvedAt = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc),
                ResolutionNotes = "Duplicate charge refunded to account",
                RefundAmount = 2000.00m
            }
        };

        modelBuilder.Entity<Dispute>().HasData(disputes);
    }
}
