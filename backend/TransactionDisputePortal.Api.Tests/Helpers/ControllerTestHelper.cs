using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TransactionDisputePortal.Api.Tests.Helpers;

public static class ControllerTestHelper
{
    /// <summary>
    /// Injects a <see cref="ClaimsPrincipal"/> into the controller's HttpContext so that
    /// User.IsInRole / User.FindFirst work correctly without running full middleware.
    /// </summary>
    public static void SetUser(ControllerBase controller,
        string role,
        int userId = 1,
        string fullName = "Test User")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
