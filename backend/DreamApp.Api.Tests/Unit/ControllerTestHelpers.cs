using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Tests.Unit;

public static class ControllerTestHelpers
{
    /// <summary>Attaches a fake authenticated identity to a controller instance, as the [Authorize] pipeline would.</summary>
    public static T AsUser<T>(this T controller, int userId) where T : ControllerBase
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}
