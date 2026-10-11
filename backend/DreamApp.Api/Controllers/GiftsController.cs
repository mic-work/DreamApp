using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gifts")]
public class GiftsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IImageStorageService? _images;

    /// <param name="images">Used to delete photo files that no gift references any more; optional so unit tests can omit it.</param>
    public GiftsController(IAppDbContext db, IImageStorageService? images = null)
    {
        _db = db;
        _images = images;
    }

    /// <summary>
    /// Lists the gifts of a wishlist. Defaults to the caller's own wishlist
    /// when wishlistId is omitted. Read access follows the same rule as
    /// GET /api/wishlists/{username}: owner or granted WishlistAccess. A
    /// non-owner only ever sees gifts that aren't restricted to other
    /// friends (see Models/GiftVisibility).
    /// </summary>
    [HttpGet]
    public ActionResult<List<GiftDto>> GetGifts([FromQuery] Guid? wishlistId)
    {
        var userId = User.GetUserId();

        Guid targetWishlistId;
        bool isOwner;
        Guid ownerId;
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

            isOwner = wishlist.OwnerId == userId;
            ownerId = wishlist.OwnerId;
        }
        else
        {
            var myWishlist = _db.Wishlists.FirstOrDefault(w => w.OwnerId == userId);
            if (myWishlist is null)
            {
                return NotFound(new ApiErrorDto("Wishlist non trovata."));
            }

            targetWishlistId = myWishlist.Id;
            isOwner = true;
            ownerId = myWishlist.OwnerId;
        }

        // Materialize the entities first, THEN sort in memory: Priority is
        // stored as a readable string ("Low"/"Medium"/"High", see
        // AppDbContext.OnModelCreating), so ordering it before materializing
        // would sort alphabetically at the SQL level (Medium, Low, High) —
        // wrong on severity. Sorting the in-memory List<Gift> instead uses
        // the actual enum values (Low=0 < Medium=1 < High=2), giving the
        // intended most-important-first order.
        var gifts = _db.Gifts
            .Where(g => g.WishlistId == targetWishlistId)
            .ToList()
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToList();

        if (!isOwner)
        {
            var visibleIds = GiftDtoFactory.FilterVisibleGiftIds(_db, gifts, userId);
            gifts = gifts.Where(g => visibleIds.Contains(g.Id)).ToList();
        }

        var dtos = gifts.Select(g => GiftDtoFactory.Build(_db, g, isOwner, userId, ownerId)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GiftDto> GetById(Guid id)
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

        var isOwner = wishlist.OwnerId == userId;
        if (!isOwner && !GiftDtoFactory.IsVisibleTo(_db, gift, userId))
        {
            return Forbid();
        }

        return Ok(GiftDtoFactory.Build(_db, gift, isOwner, userId, wishlist.OwnerId));
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
            // Legacy single-photo field: no longer written by new code, see SavePhotos/GiftPhoto.
            ImageUrl = null,
            ProductUrl = dto.ProductUrl,
            Priority = dto.Priority,
            VisibleToAll = dto.VisibleToAll,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Add(gift);
        await _db.SaveChangesAsync();

        SavePhotos(gift.Id, dto.ImageUrls);
        SaveVisibility(gift.Id, userId, dto.VisibleToAll, dto.VisibleToUserIds);
        await _db.SaveChangesAsync();

        var result = GiftDtoFactory.Build(_db, gift, isOwner: true, callerId: userId, ownerId: userId);
        return CreatedAtAction(nameof(GetById), new { id = gift.Id }, result);
    }

    /// <summary>
    /// Updates a gift. Ownership is re-checked on the SERVER using the
    /// gift's own wishlist, never trusting any id supplied by the client
    /// beyond the {id} in the route: a user cannot edit gift 123 just by
    /// guessing/changing the id if that gift belongs to someone else's wishlist.
    /// Photos and the visibility list are fully replaced with whatever the
    /// client sends (simpler and safer than diffing individual rows).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GiftDto>> Update(Guid id, GiftUpdateDto dto)
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
        gift.ProductUrl = dto.ProductUrl;
        gift.Priority = dto.Priority;
        gift.VisibleToAll = dto.VisibleToAll;
        gift.UpdatedAt = DateTime.UtcNow;

        var previousPhotos = _db.GiftPhotos.Where(p => p.GiftId == gift.Id).ToList();
        var previousPhotoUrls = previousPhotos.Select(p => p.Url).ToList();
        _db.RemoveRange(previousPhotos);
        _db.RemoveRange(_db.GiftVisibilities.Where(v => v.GiftId == gift.Id).ToList());

        SavePhotos(gift.Id, dto.ImageUrls);
        SaveVisibility(gift.Id, userId, dto.VisibleToAll, dto.VisibleToUserIds);

        await _db.SaveChangesAsync();

        // Photos the owner removed while editing no longer belong to any gift: free the disk space.
        DeleteOrphanedPhotos(previousPhotoUrls);

        return Ok(GiftDtoFactory.Build(_db, gift, isOwner: true, callerId: userId, ownerId: userId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
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

        var photos = _db.GiftPhotos.Where(p => p.GiftId == gift.Id).ToList();
        var photoUrls = photos.Select(p => p.Url).ToList();
        if (!string.IsNullOrWhiteSpace(gift.ImageUrl))
        {
            photoUrls.Add(gift.ImageUrl);
        }

        _db.RemoveRange(photos);
        _db.Remove(gift);
        await _db.SaveChangesAsync();

        DeleteOrphanedPhotos(photoUrls);

        return NoContent();
    }

    /// <summary>
    /// Lets the caller record whether this gift has already been given, and
    /// by whom. The OWNER's record is definitive: naming a friend as the
    /// giver immediately confirms it for that friend too, with no action
    /// needed on their side (see GiftDtoFactory.Build). A FRIEND's record is
    /// always a self-claim ("I already gave this") — the client's own
    /// GivenByUserId is ignored for a non-owner, since it can only ever be
    /// the caller themselves — and stays a private, retractable "pending"
    /// signal until the owner corroborates it: two people never confirm a
    /// gift from one person's say-so. See Models/GiftReception.
    /// </summary>
    [HttpPut("{id:guid}/reception")]
    public async Task<ActionResult<GiftDto>> SetReception(Guid id, GiftReceptionRequestDto dto)
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

        var isOwner = wishlist.OwnerId == userId;

        // A restricted gift's visibility list never includes the owner
        // themselves, so this check only makes sense for a friend looking
        // at someone else's gift — the owner can always mark their own.
        if (!isOwner && !GiftDtoFactory.IsVisibleTo(_db, gift, userId))
        {
            return Forbid();
        }

        var existing = _db.GiftReceptions.FirstOrDefault(r => r.GiftId == gift.Id && r.MarkedByUserId == userId);

        if (!dto.Received)
        {
            if (existing is not null)
            {
                _db.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return Ok(GiftDtoFactory.Build(_db, gift, isOwner, callerId: userId, ownerId: wishlist.OwnerId));
        }

        Guid givenByUserId;
        if (isOwner)
        {
            if (!dto.GivenByUserId.HasValue)
            {
                return BadRequest(new ApiErrorDto("Indica da chi è stato ricevuto questo regalo."));
            }

            // The giver must be someone who can actually see this wishlist — never trust an arbitrary id from the client.
            var validGiverIds = _db.WishlistAccesses.Where(a => a.WishlistId == wishlist.Id).Select(a => a.UserId).ToHashSet();
            if (!validGiverIds.Contains(dto.GivenByUserId.Value))
            {
                return BadRequest(new ApiErrorDto("La persona indicata non ha accesso a questa lista."));
            }

            givenByUserId = dto.GivenByUserId.Value;
        }
        else
        {
            // A friend can only ever claim to have given the gift
            // themselves, never on someone else's behalf — whoever ends up
            // being the confirmed giver is always the OWNER's call.
            givenByUserId = userId;
        }

        if (existing is null)
        {
            _db.Add(new GiftReception
            {
                GiftId = gift.Id,
                MarkedByUserId = userId,
                GivenByUserId = givenByUserId,
                ReceivedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.GivenByUserId = givenByUserId;
            existing.ReceivedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(GiftDtoFactory.Build(_db, gift, isOwner, callerId: userId, ownerId: wishlist.OwnerId));
    }

    /// <summary>Deletes the files of the given photo URLs, unless some gift still uses them. Call it AFTER the database changes are saved.</summary>
    private void DeleteOrphanedPhotos(IEnumerable<string> candidateUrls)
    {
        if (_images is null)
        {
            return;
        }

        foreach (var url in candidateUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
        {
            var stillUsed = _db.GiftPhotos.Any(p => p.Url == url) || _db.Gifts.Any(g => g.ImageUrl == url);
            if (!stillUsed)
            {
                _images.Delete(url);
            }
        }
    }

    private bool CanRead(Wishlist wishlist, Guid userId) =>
        wishlist.OwnerId == userId ||
        _db.WishlistAccesses.Any(a => a.WishlistId == wishlist.Id && a.UserId == userId);

    /// <summary>Persists the gift's photo gallery (in order) and returns the cleaned list of URLs actually saved.</summary>
    private List<string> SavePhotos(Guid giftId, List<string> imageUrls)
    {
        var cleaned = imageUrls
            .Select(u => u?.Trim())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .ToList();

        for (var i = 0; i < cleaned.Count; i++)
        {
            _db.Add(new GiftPhoto { GiftId = giftId, Url = cleaned[i], SortOrder = i, CreatedAt = DateTime.UtcNow });
        }

        return cleaned;
    }

    /// <summary>
    /// Persists the gift's visibility list and returns the ids actually
    /// saved. When the gift is shared with everyone (<paramref name="visibleToAll"/>)
    /// no rows are stored at all: the flag alone says "every friend, even
    /// future ones". When it is not, the list is exactly who can see it, and
    /// an empty list means nobody but the owner. Only ids the OWNER has personally added as a friend
    /// (i.e. whose wishlist the owner can see — same pool as "Liste dei miei
    /// amici") are accepted — never the people who merely added the owner.
    /// This is what lets you designate "this gift is for Michele" even
    /// before Michele has added you back: never trust arbitrary ids from
    /// the client regardless.
    /// </summary>
    private List<Guid> SaveVisibility(Guid giftId, Guid ownerId, bool visibleToAll, List<Guid> requestedUserIds)
    {
        if (visibleToAll)
        {
            return new List<Guid>();
        }

        var myFriendIds = _db.WishlistAccesses
            .Where(a => a.UserId == ownerId)
            .Join(_db.Wishlists, a => a.WishlistId, w => w.Id, (a, w) => w.OwnerId)
            .ToHashSet();

        var accepted = requestedUserIds.Distinct().Where(myFriendIds.Contains).ToList();

        foreach (var id in accepted)
        {
            _db.Add(new GiftVisibility { GiftId = giftId, UserId = id, CreatedAt = DateTime.UtcNow });
        }

        return accepted;
    }
}
