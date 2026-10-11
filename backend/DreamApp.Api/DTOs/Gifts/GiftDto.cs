using DreamApp.Api.Models;

namespace DreamApp.Api.DTOs.Gifts;

public class GiftDto
{
    public Guid Id { get; set; }
    public Guid WishlistId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }

    /// <summary>Deprecated: kept only for older clients. New clients should use <see cref="Photos"/>.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Photo gallery, in display order. Falls back to the single legacy ImageUrl for gifts saved before multi-photo support.</summary>
    public List<string> Photos { get; set; } = new();

    public string? ProductUrl { get; set; }
    public GiftPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>True when the caller owns this gift (i.e. it's in their own wishlist).</summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Only meaningful when IsOwner is true: which friends this gift is
    /// restricted to. Empty means "visible to every friend who can see the
    /// wishlist" (the default).
    /// </summary>
    public List<Guid> VisibleToUserIds { get; set; } = new();

    /// <summary>
    /// Only meaningful when IsOwner is true: true = the gift is shared with
    /// every friend (VisibleToUserIds is then empty); false = shared only
    /// with VisibleToUserIds, and an empty list means nobody but the owner.
    /// </summary>
    public bool VisibleToAll { get; set; }

    /// <summary>
    /// Whether this gift has been definitively confirmed as received/given.
    /// When IsOwner is true: the owner recorded "I already received this,
    /// from X" — immediate, no corroboration needed (ReceivedFromUserId /
    /// ReceivedFromUsername say who). When IsOwner is false: the OWNER has
    /// named the caller as the giver — the caller's own claim alone is
    /// never enough (see MyClaimPending and Models/GiftReception).
    /// </summary>
    public bool ReceivedByMe { get; set; }

    /// <summary>Only meaningful when IsOwner is true — who the owner says gave this gift.</summary>
    public Guid? ReceivedFromUserId { get; set; }
    public string? ReceivedFromUsername { get; set; }

    /// <summary>
    /// Only meaningful when ReceivedByMe is true: when the gift was
    /// definitively confirmed as received/given (the owner's confirming
    /// GiftReception row's timestamp) — lets a "già regalati"/"ricevuti"
    /// list be sorted by confirmation date instead of priority.
    /// </summary>
    public DateTime? ReceivedAt { get; set; }

    /// <summary>
    /// Only meaningful when IsOwner is false: the caller has privately
    /// claimed "I already gave this", but the owner hasn't confirmed it yet
    /// (ReceivedByMe is still false). A pending claim can be retracted the
    /// same way it was made (PUT .../reception with received:false).
    /// </summary>
    public bool MyClaimPending { get; set; }

    /// <summary>
    /// Only meaningful when IsOwner is true: ids of friends who have
    /// privately claimed "I already gave this" but aren't confirmed yet —
    /// a hint for "chi l'ha regalato?", never a confirmation by itself.
    /// </summary>
    public List<Guid> ClaimedByUserIds { get; set; } = new();
}
