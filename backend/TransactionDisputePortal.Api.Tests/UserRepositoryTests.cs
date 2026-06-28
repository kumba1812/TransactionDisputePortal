using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Repositories.User;

namespace TransactionDisputePortal.Api.Tests;

public class UserRepositoryTests
{
    // Fresh context — no seed data, so no ID conflicts
    private static ApplicationDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationUser NewUser(string username = "testuser", string role = "Client",
        bool isActive = true) => new()
    {
        Username = username,
        PasswordHash = "hash",
        FullName = "Test User",
        Role = role,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddUser_Stores_And_ReturnsUser()
    {
        var repo = new UserRepository(CreateFreshContext());
        var user = NewUser("alice");

        var added = await repo.AddAsync(user);

        Assert.True(added.Id > 0);
        Assert.Equal("alice", added.Username);
    }

    [Fact]
    public async Task GetByUsername_ReturnsActiveUser()
    {
        var repo = new UserRepository(CreateFreshContext());
        await repo.AddAsync(NewUser("bob", isActive: true));

        var found = await repo.GetByUsernameAsync("bob");

        Assert.NotNull(found);
        Assert.Equal("bob", found!.Username);
    }

    [Fact]
    public async Task GetByUsername_ReturnsNull_WhenNotFound()
    {
        var repo = new UserRepository(CreateFreshContext());

        var found = await repo.GetByUsernameAsync("nobody");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByUsername_ReturnsNull_WhenInactive()
    {
        var repo = new UserRepository(CreateFreshContext());
        await repo.AddAsync(NewUser("charlie", isActive: false));

        var found = await repo.GetByUsernameAsync("charlie");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetById_ReturnsUser()
    {
        var repo = new UserRepository(CreateFreshContext());
        var added = await repo.AddAsync(NewUser("diana"));

        var found = await repo.GetByIdAsync(added.Id);

        Assert.NotNull(found);
        Assert.Equal(added.Id, found!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenMissing()
    {
        var repo = new UserRepository(CreateFreshContext());

        var found = await repo.GetByIdAsync(99999);

        Assert.Null(found);
    }
}
