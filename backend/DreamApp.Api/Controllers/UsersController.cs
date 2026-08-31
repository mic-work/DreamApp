using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAppDbContext _db;

    public UsersController(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Looks up a user by username ("Aggiungi amico" search). Returns only
    /// public information (never the email) plus the id of that user's
    /// personal wishlist, which the caller can then request access to via
    /// POST /api/wishlists/{wishlistId}/access.
    /// </summary>
    [HttpGet("{username}")]
    public ActionResult<UserPublicDto> GetByUsername(string username)
    {
        var normalized = username.Trim();
        var user = _db.Users.FirstOrDefault(u => u.Username.ToLower() == normalized.ToLower());
        if (user is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        var wishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == user.Id);
        if (wishlist is null)
        {
            // Should never happen (a wishlist is always created at registration), but fail clearly if it does.
            return NotFound(new ApiErrorDto("Questo utente non ha ancora una wishlist."));
        }

        return Ok(new UserPublicDto
        {
            Id = user.Id,
            Username = user.Username,
            WishlistId = wishlist.Id
        });
    }
}
