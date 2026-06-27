using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using TransactionDisputePortal.Api.Controllers;
using TransactionDisputePortal.Api.Models;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Tests.Helpers;
using Xunit;

namespace TransactionDisputePortal.Api.Tests;

public class AuthControllerTests
{
    private const string JwtKey = "TestKeyForUnitTests-MustBe32Chars!!";

    private static (AuthController controller,
        Mock<IUserRepository> repoMock,
        Mock<IPasswordHasher<ApplicationUser>> hasherMock)
        BuildController()
    {
        var repoMock = new Mock<IUserRepository>();
        var hasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns((string?)JwtKey);
        configMock.Setup(c => c["Jwt:Issuer"]).Returns((string?)"TestIssuer");

        var controller = new AuthController(repoMock.Object, hasherMock.Object, configMock.Object);
        return (controller, repoMock, hasherMock);
    }

    private static ApplicationUser ActiveUser(int id = 1, string role = "Client",
        string username = "testuser") => new()
    {
        Id = id,
        Username = username,
        PasswordHash = "hashed",
        FullName = "Test User",
        Role = role,
        IsActive = true
    };

    [Fact]
    public async Task Login_ValidAdmin_Returns200WithRole()
    {
        var (controller, repo, hasher) = BuildController();
        var user = ActiveUser(1, "Admin", "admin");
        repo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        hasher.Setup(h => h.VerifyHashedPassword(user, "hashed", "pass"))
              .Returns(PasswordVerificationResult.Success);

        var result = await controller.Login(new LoginRequest { Username = "admin", Password = "pass" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value as LoginResponse;
        Assert.NotNull(body);
        Assert.Equal("Admin", body!.Role);
        Assert.NotEmpty(body.AccessToken);
    }

    [Fact]
    public async Task Login_ValidBanker_Returns200WithRole()
    {
        var (controller, repo, hasher) = BuildController();
        var user = ActiveUser(2, "Banker", "banker");
        repo.Setup(r => r.GetByUsernameAsync("banker")).ReturnsAsync(user);
        hasher.Setup(h => h.VerifyHashedPassword(user, "hashed", "pass"))
              .Returns(PasswordVerificationResult.Success);

        var result = await controller.Login(new LoginRequest { Username = "banker", Password = "pass" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Banker", ((LoginResponse)ok.Value!).Role);
    }

    [Fact]
    public async Task Login_ValidClient_Returns200WithRole()
    {
        var (controller, repo, hasher) = BuildController();
        var user = ActiveUser(4, "Client", "client");
        repo.Setup(r => r.GetByUsernameAsync("client")).ReturnsAsync(user);
        hasher.Setup(h => h.VerifyHashedPassword(user, "hashed", "pass"))
              .Returns(PasswordVerificationResult.Success);

        var result = await controller.Login(new LoginRequest { Username = "client", Password = "pass" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Client", ((LoginResponse)ok.Value!).Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (controller, repo, hasher) = BuildController();
        var user = ActiveUser();
        repo.Setup(r => r.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        hasher.Setup(h => h.VerifyHashedPassword(user, "hashed", "wrong"))
              .Returns(PasswordVerificationResult.Failed);

        var result = await controller.Login(new LoginRequest { Username = "testuser", Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_UnknownUsername_Returns401()
    {
        var (controller, repo, _) = BuildController();
        repo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await controller.Login(new LoginRequest { Username = "ghost", Password = "x" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyUsername_Returns400()
    {
        var (controller, _, _) = BuildController();

        var result = await controller.Login(new LoginRequest { Username = "   ", Password = "pass" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        var (controller, _, _) = BuildController();

        var result = await controller.Login(new LoginRequest { Username = "user", Password = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
