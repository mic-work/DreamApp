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
    /// <summary>Upper bound of the "Aggiungi amico" search results, so a one-letter query can't return the whole user table.</summary>
    private const int MaxSearchResults = 20;

    private readonly IAppDbContext _db;

    public UsersController(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Searches users whose username CONTAINS the given text, ignoring case
    /// ("mic" or "ele" both find "michele") — the "Aggiungi amico" popup
    /// shows the matches as a list. Same public-only data as
    /// <see cref="GetByUsername"/> (never the email). The caller is never part
    /// of the results (you can't add yourself), and the list is capped at
    /// <see cref="MaxSearchResults"/>. An empty/blank query returns an empty list.
    ///
    /// The literal "search" segment wins over the "{username}" route below.
    /// </summary>
    [HttpGet("search")]
    public ActionResult<IEnumerable<UserPublicDto>> Search([FromQuery] string? q)
    {
        var term = (q ?? string.Empty).Trim().ToLower();
        if (term.Length == 0)
        {
            return Ok(new List<UserPublicDto>());
        }

        var callerId = User.GetUserId();

        var matches = _db.Users
            .Where(u => u.Id != callerId && u.Username.ToLower().Contains(term))
            .OrderBy(u => u.Username)
            .Take(MaxSearchResults)
            .ToList();

        var matchIds = matches.Select(u => u.Id).ToList();
        var wishlistIdByOwner = _db.Wishlists
            .Where(w => matchIds.Contains(w.OwnerId))
            .ToList()
            .GroupBy(w => w.OwnerId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        // A wishlist is always created at registration; a user without one (should never happen) just can't be added, so skip it.
        var results = matches
            .Where(u => wishlistIdByOwner.ContainsKey(u.Id))
            .Select(u => new UserPublicDto
            {
                Id = u.Id,
                Username = u.Username,
                WishlistId = wishlistIdByOwner[u.Id]
            })
            .ToList();

        return Ok(results);
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
