using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.DTOs.Wishlists;
using DreamApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlists")]
public class WishlistsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public WishlistsController(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>The authenticated user's own wishlist, always fully editable.</summary>
    [HttpGet("me")]
    public ActionResult<WishlistDetailDto> GetMyWishlist()
    {
        var userId = User.GetUserId();
        var wishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == userId);
        if (wishlist is null)
        {
            return NotFound(new ApiErrorDto("Wishlist non trovata."));
        }

        return Ok(BuildDetail(wishlist, userId));
    }

    /// <summary>
    /// A wishlist belonging to another user, identified by username.
    /// Only the owner or a user with a WishlistAccess grant can read it,
    /// and even then strictly read-only (IsOwner will be false).
    /// </summary>
    [HttpGet("{username}")]
    public ActionResult<WishlistDetailDto> GetByUsername(string username)
    {
        var userId = User.GetUserId();
        var normalized = username.Trim();

        var owner = _db.Users.FirstOrDefault(u => u.Username.ToLower() == normalized.ToLower());
        if (owner is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        var wishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == owner.Id);
        if (wishlist is null)
        {
            return NotFound(new ApiErrorDto("Questo utente non ha ancora una wishlist."));
        }

        var isOwner = wishlist.OwnerId == userId;
        var hasAccess = isOwner || _db.WishlistAccesses.Any(a => a.WishlistId == wishlist.Id && a.UserId == userId);
        if (!hasAccess)
        {
            return Forbid();
        }

        return Ok(BuildDetail(wishlist, userId));
    }

    /// <summary>Wishlists of other users the caller has been granted access to ("Liste dei miei amici").</summary>
    [HttpGet("friends")]
    public ActionResult<List<WishlistSummaryDto>> GetFriendsWishlists()
    {
        var userId = User.GetUserId();

        var summaries = _db.WishlistAccesses
            .Where(a => a.UserId == userId)
            .Join(_db.Wishlists, a => a.WishlistId, w => w.Id, (a, w) => w)
            .Select(w => new WishlistSummaryDto
            {
                Id = w.Id,
                OwnerUsername = w.Owner.Username,
                Name = w.Name,
                GiftsCount = _db.Gifts.Count(g => g.WishlistId == w.Id)
            })
            .ToList();

        return Ok(summaries);
    }

    /// <summary>Grants the caller read access to someone else's wishlist ("Aggiungi lista amico").</summary>
    [HttpPost("{wishlistId:int}/access")]
    public async Task<IActionResult> GrantAccessToSelf(int wishlistId)
    {
        var userId = User.GetUserId();
        var wishlist = _db.Wishlists.FirstOrDefault(w => w.Id == wishlistId);
        if (wishlist is null)
        {
            return NotFound(new ApiErrorDto("Wishlist non trovata."));
        }

        if (wishlist.OwnerId == userId)
        {
            return BadRequest(new ApiErrorDto("Non puoi aggiungere la tua stessa wishlist."));
        }

        if (_db.WishlistAccesses.Any(a => a.WishlistId == wishlistId && a.UserId == userId))
        {
            return Conflict(new ApiErrorDto("Hai già accesso a questa wishlist."));
        }

        _db.Add(new WishlistAccess
        {
            UserId = userId,
            WishlistId = wishlistId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Removes the caller's access to someone else's wishlist. Never touches the owner's data.</summary>
    [HttpDelete("{wishlistId:int}/access")]
    public async Task<IActionResult> RevokeAccessToSelf(int wishlistId)
    {
        var userId = User.GetUserId();
        var access = _db.WishlistAccesses.FirstOrDefault(a => a.WishlistId == wishlistId && a.UserId == userId);
        if (access is null)
        {
            return NotFound(new ApiErrorDto("Non hai accesso a questa wishlist."));
        }

        _db.Remove(access);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private WishlistDetailDto BuildDetail(Wishlist wishlist, int callerId)
    {
        var owner = wishlist.Owner ?? _db.Users.First(u => u.Id == wishlist.OwnerId);

        var gifts = _db.Gifts
            .Where(g => g.WishlistId == wishlist.Id)
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .Select(g => new GiftDto
            {
                Id = g.Id,
                WishlistId = g.WishlistId,
                Name = g.Name,
                Description = g.Description,
                Notes = g.Notes,
                ImageUrl = g.ImageUrl,
                ProductUrl = g.ProductUrl,
                Priority = g.Priority,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToList();

        return new WishlistDetailDto
        {
            Id = wishlist.Id,
            OwnerId = wishlist.OwnerId,
            OwnerUsername = owner.Username,
            Name = wishlist.Name,
            CreatedAt = wishlist.CreatedAt,
            IsOwner = wishlist.OwnerId == callerId,
            Gifts = gifts
        };
    }
}
