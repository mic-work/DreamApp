using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Users;
using DreamApp.Api.Models;
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
    ///
    /// A user can never find/add themselves this way: searching your own
    /// username is explicitly rejected rather than returning your own profile.
    /// </summary>
    [HttpGet("{username}")]
    public ActionResult<UserPublicDto> GetByUsername(string username)
    {
        var normalized = username.Trim();
        var callerId = User.GetUserId();

        var user = _db.Users.FirstOrDefault(u => u.Username.ToLower() == normalized.ToLower());
        if (user is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        if (user.Id == callerId)
        {
            return BadRequest(new ApiErrorDto("Non puoi cercare o aggiungere te stesso come amico."));
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

    /// <summary>
    /// Updates the personal data (username, email) of the currently
    /// authenticated user. Never trusts an id from the client: the target is
    /// always the caller, taken from the JWT claim.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateCurrentUser(UserUpdateDto dto)
    {
        var userId = User.GetUserId();
        var user = _db.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        var normalizedUsername = dto.Username.Trim();
        var normalizedEmail = dto.Email.Trim();

        if (_db.Users.Any(u => u.Id != userId && u.Username.ToLower() == normalizedUsername.ToLower()))
        {
            return Conflict(new ApiErrorDto("Questo username è già in uso."));
        }

        if (_db.Users.Any(u => u.Id != userId && u.Email.ToLower() == normalizedEmail.ToLower()))
        {
            return Conflict(new ApiErrorDto("Questa email è già registrata."));
        }

        user.Username = normalizedUsername;
        user.Email = normalizedEmail;
        await _db.SaveChangesAsync();

        return Ok(ToUserDto(user));
    }

    private static UserDto ToUserDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    };
}
