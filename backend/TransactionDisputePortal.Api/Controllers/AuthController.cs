using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models.User;
using TransactionDisputePortal.Api.Repositories.User;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);

            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with missing username or password");
                return BadRequest(new { message = "Username and password are required" });
            }

            var user = await _userRepository.GetByUsernameAsync(request.Username.Trim().ToLowerInvariant());
            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found for username {Username}", request.Username);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed: incorrect password for user {Username}", request.Username);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role),
            };

            var key = _config["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "ChangeMeInProductionKey123!";
            var issuer = _config["Jwt:Issuer"] ?? "TransactionDisputePortal";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            _logger.LogInformation("Login successful for user {UserId} ({Username})", user.Id, user.Username);

            return Ok(new LoginResponse
            {
                AccessToken = token,
                ExpiresAt = tokenDescriptor.ValidTo,
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login for username {Username}", request?.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during login" });
        }
    }
}
