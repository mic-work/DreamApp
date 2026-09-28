using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;

namespace DreamApp.Api.Common;

/// <summary>
/// Builds a GiftDto (photos, visibility, reception status) and answers
/// visibility questions. Shared by GiftsController and WishlistsController
/// so the two never drift apart on what a gift "looks like" to a given caller.
/// </summary>
public static class GiftDtoFactory
{
    public static GiftDto Build(IAppDbContext db, Gift gift, bool isOwner, Guid callerId, Guid ownerId)
    {
        var photos = db.GiftPhotos
            .Where(p => p.GiftId == gift.Id)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.Url)
            .ToList();

        // Fallback for gifts saved before multi-photo support: show the old
        // single ImageUrl as the gift's only photo instead of an empty gallery.
        var effectivePhotos = photos.Count > 0
            ? photos
            : (gift.ImageUrl is not null ? new List<string> { gift.ImageUrl } : new List<string>());

        // Only the owner needs to know the current restriction list (to
        // prefill the edit form) — irrelevant, and never exposed, to anyone
        // else looking at the gift.
        var visibleToUserIds = isOwner
            ? db.GiftVisibilities.Where(v => v.GiftId == gift.Id).Select(v => v.UserId).ToList()
            : new List<Guid>();

        var receivedByMe = false;
        Guid? receivedFromUserId = null;
        string? receivedFromUsername = null;
        DateTime? receivedAt = null;
        var myClaimPending = false;
        var claimedByUserIds = new List<Guid>();

        if (isOwner)
        {
            // The owner's own record is the DEFINITIVE confirmation: "I
            // already received this, from X". Setting it is immediate and
            // needs no corroboration from anyone else.
            var ownerReception = db.GiftReceptions.FirstOrDefault(r => r.GiftId == gift.Id && r.MarkedByUserId == callerId);
            if (ownerReception is not null)
            {
                receivedByMe = true;
                receivedFromUserId = ownerReception.GivenByUserId;
                receivedFromUsername = db.Users.FirstOrDefault(u => u.Id == ownerReception.GivenByUserId)?.Username;
                receivedAt = ownerReception.ReceivedAt;
            }

            // Friends who have privately claimed "I already gave this" (a
            // self-claim: MarkedByUserId == GivenByUserId), not yet
            // confirmed by the owner. Surfaced only to the owner, as a hint
            // for "chi l'ha regalato?" — never a confirmation by itself, and
            // only relevant BEFORE the owner has confirmed: once
            // receivedByMe is true there's nothing left to hint at, and a
            // leftover self-claim (the friend never retracted it) shouldn't
            // keep flagging an already-settled gift.
            if (!receivedByMe)
            {
                claimedByUserIds = db.GiftReceptions
                    .Where(r => r.GiftId == gift.Id && r.MarkedByUserId == r.GivenByUserId && r.MarkedByUserId != ownerId)
                    .Select(r => r.MarkedByUserId)
                    .ToList();
            }
        }
        else
        {
            // A friend's own claim NEVER confirms itself, however sure they
            // are: the definitive word is always the owner's own reception
            // record naming this caller as the giver (see above). Without
            // that, the caller's claim stays a private, retractable "I
            // think I already gave this" — see Models/GiftReception.
            var ownerConfirmation = db.GiftReceptions.FirstOrDefault(r => r.GiftId == gift.Id && r.MarkedByUserId == ownerId);
            receivedByMe = ownerConfirmation is not null && ownerConfirmation.GivenByUserId == callerId;
            if (receivedByMe)
            {
                receivedAt = ownerConfirmation!.ReceivedAt;
            }

            var myClaim = db.GiftReceptions.FirstOrDefault(r => r.GiftId == gift.Id && r.MarkedByUserId == callerId);
            myClaimPending = myClaim is not null && !receivedByMe;
        }

        return new GiftDto
        {
            Id = gift.Id,
            WishlistId = gift.WishlistId,
            Name = gift.Name,
            Description = gift.Description,
            Notes = gift.Notes,
            ImageUrl = gift.ImageUrl,
            Photos = effectivePhotos,
            ProductUrl = gift.ProductUrl,
            Priority = gift.Priority,
            CreatedAt = gift.CreatedAt,
            UpdatedAt = gift.UpdatedAt,
            IsOwner = isOwner,
            VisibleToUserIds = visibleToUserIds,
            ReceivedByMe = receivedByMe,
            ReceivedFromUserId = receivedFromUserId,
            ReceivedFromUsername = receivedFromUsername,
            ReceivedAt = receivedAt,
            MyClaimPending = myClaimPending,
            ClaimedByUserIds = claimedByUserIds
        };
    }

    /// <summary>True when a gift has no visibility restriction, or the given user is one of the friends it's restricted to.</summary>
    public static bool IsVisibleTo(IAppDbContext db, Guid giftId, Guid userId)
    {
        var isRestricted = db.GiftVisibilities.Any(v => v.GiftId == giftId);
        return !isRestricted || db.GiftVisibilities.Any(v => v.GiftId == giftId && v.UserId == userId);
    }

    /// <summary>Filters a batch of gift ids down to the ones visible to the given (non-owner) user.</summary>
    public static HashSet<Guid> FilterVisibleGiftIds(IAppDbContext db, IEnumerable<Guid> giftIds, Guid userId)
    {
        var idSet = giftIds.ToHashSet();
        var restrictedGiftIds = db.GiftVisibilities.Where(v => idSet.Contains(v.GiftId)).Select(v => v.GiftId).ToHashSet();
        var allowedForMeGiftIds = db.GiftVisibilities
            .Where(v => v.UserId == userId && idSet.Contains(v.GiftId))
            .Select(v => v.GiftId)
            .ToHashSet();

        return idSet.Where(id => !restrictedGiftIds.Contains(id) || allowedForMeGiftIds.Contains(id)).ToHashSet();
    }
}
