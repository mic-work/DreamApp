using System.Security.Claims;

namespace DreamApp.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's database Id from the JWT "sub"/NameIdentifier claim.
    /// Throws if called on a principal that isn't authenticated — controllers using this must
    /// be decorated with [Authorize], which guarantees the claim is present.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !int.TryParse(value, out var id))
        {
            throw new InvalidOperationException("The current principal does not have a valid user id claim.");
        }

        return id;
    }
}
