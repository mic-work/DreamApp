using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gifts")]
public class GiftsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public GiftsController(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists the gifts of a wishlist. Defaults to the caller's own wishlist
    /// when wishlistId is omitted. Read access follows the same rule as
    /// GET /api/wishlists/{username}: owner or granted WishlistAccess.
    /// </summary>
    [HttpGet]
    public ActionResult<List<GiftDto>> GetGifts([FromQuery] int? wishlistId)
    {
        var userId = User.GetUserId();

        int targetWishlistId;
        if (wishlistId.HasValue)
        {
            targetWishlistId = wishlistId.Value;
            var wishlist = _db.Wishlists.FirstOrDefault(w => w.Id == targetWishlistId);
            if (wishlist is null)
            {
                return NotFound(new ApiErrorDto("Wishlist non trovata."));
            }

            if (!CanRead(wishlist, userId))
            {
                return Forbid();
            }
        }
        else
        {
            var myWishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == userId);
            if (myWishlist is null)
            {
                return NotFound(new ApiErrorDto("Wishlist non trovata."));
            }

            targetWishlistId = myWishlist.Id;
        }

        // Materialize the entities first, then map to DTOs in memory: keeps
        // the mapping logic in one place (ToDto) without relying on the
        // query provider being able to translate an arbitrary method call.
        var gifts = _db.Gifts
            .Where(g => g.WishlistId == targetWishlistId)
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToList()
            .Select(ToDto)
            .ToList();

        return Ok(gifts);
    }

    [HttpGet("{id:int}")]
    public ActionResult<GiftDto> GetById(int id)
    {
        var userId = User.GetUserId();
        var gift = _db.Gifts.FirstOrDefault(g => g.Id == id);
        if (gift is null)
        {
            return NotFound(new ApiErrorDto("Regalo non trovato."));
        }

        var wishlist = _db.Wishlists.FirstOrDefault(w => w.Id == gift.WishlistId);
        if (wishlist is null || !CanRead(wishlist, userId))
        {
            return Forbid();
        }

        return Ok(ToDto(gift));
    }

    /// <summary>Creates a gift in the caller's own wishlist. A user can never create a gift for someone else.</summary>
    [HttpPost]
    public async Task<ActionResult<GiftDto>> Create(GiftCreateDto dto)
    {
        var userId = User.GetUserId();
        var myWishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == userId);
        if (myWishlist is null)
        {
            return NotFound(new ApiErrorDto("Wishlist non trovata."));
        }

        var gift = new Gift
        {
            WishlistId = myWishlist.Id,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Notes = dto.Notes,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            Priority = dto.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Add(gift);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = gift.Id }, ToDto(gift));
    }

    /// <summary>
    /// Updates a gift. Ownership is re-checked on the SERVER using the
    /// gift's own wishlist, never trusting any id supplied by the client
    /// beyond the {id} in the route: a user cannot edit gift 123 just by
    /// guessing/changing the id if that gift belongs to someone else's wishlist.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GiftDto>> Update(int id, GiftUpdateDto dto)
    {
        var userId = User.GetUserId();
        var gift = _db.Gifts.FirstOrDefault(g => g.Id == id);
        if (gift is null)
        {
            return NotFound(new ApiErrorDto("Regalo non trovato."));
        }

        var wishlist = _db.Wishlists.FirstOrDefault(w => w.Id == gift.WishlistId);
        if (wishlist is null || wishlist.OwnerId != userId)
        {
            return Forbid();
        }

        gift.Name = dto.Name.Trim();
        gift.Description = dto.Description;
        gift.Notes = dto.Notes;
        gift.ImageUrl = dto.ImageUrl;
        gift.ProductUrl = dto.ProductUrl;
        gift.Priority = dto.Priority;
        gift.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ToDto(gift));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var gift = _db.Gifts.FirstOrDefault(g => g.Id == id);
        if (gift is null)
        {
            return NotFound(new ApiErrorDto("Regalo non trovato."));
        }

        var wishlist = _db.Wishlists.FirstOrDefault(w => w.Id == gift.WishlistId);
        if (wishlist is null || wishlist.OwnerId != userId)
        {
            return Forbid();
        }

        _db.Remove(gift);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool CanRead(Wishlist wishlist, int userId) =>
        wishlist.OwnerId == userId ||
        _db.WishlistAccesses.Any(a => a.WishlistId == wishlist.Id && a.UserId == userId);

    private static GiftDto ToDto(Gift gift) => new()
    {
        Id = gift.Id,
        WishlistId = gift.WishlistId,
        Name = gift.Name,
        Description = gift.Description,
        Notes = gift.Notes,
        ImageUrl = gift.ImageUrl,
        ProductUrl = gift.ProductUrl,
        Priority = gift.Priority,
        CreatedAt = gift.CreatedAt,
        UpdatedAt = gift.UpdatedAt
    };
}
