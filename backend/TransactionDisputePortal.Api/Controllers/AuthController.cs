using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TransactionDisputePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "username and password required" });

        // Demo credentials - replace with real user store in production
        int userId;
        var claims = new List<Claim>();
        if (request.Username == "demo" && request.Password == "demo")
        {
            userId = 1;
            claims.Add(new Claim(ClaimTypes.Name, "Demo User"));
        }
        else if (request.Username == "admin" && request.Password == "admin")
        {
            userId = 2;
            claims.Add(new Claim(ClaimTypes.Name, "Admin User"));
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        else
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Ensure NameIdentifier claim is present (used by controllers)
        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var key = _config["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "ChangeMeInProductionKey123!";
        var issuer = _config["Jwt:Issuer"] ?? "TransactionDisputePortal";
        var expiresInHours = 24;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresInHours),
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = tokenDescriptor.ValidTo,
            UserId = userId
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
}
