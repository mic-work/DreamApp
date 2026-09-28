namespace DreamApp.Api.Models;

/// <summary>
/// Records that, according to <see cref="MarkedByUserId"/>, this gift has
/// already been given to the wishlist owner by <see cref="GivenByUserId"/>.
/// Two very different roles share this table:
/// <list type="bullet">
/// <item>
/// The wishlist OWNER's own row (MarkedByUserId == the owner) is the
/// DEFINITIVE confirmation: "I already received this, from X". It takes
/// effect immediately, and automatically marks the gift as received/given
/// from X's own point of view too — X doesn't need to do anything.
/// </item>
/// <item>
/// A FRIEND's own row is always a self-claim (MarkedByUserId ==
/// GivenByUserId == that friend): "I think I already gave this". It is
/// PRIVATE to that friend and never confirms anything by itself — it stays
/// a retractable "pending" signal until the owner's own row corroborates it
/// by naming the same friend as the giver. This is intentional: whichever
/// of the two acts first, only the owner's word is definitive, so a friend
/// can never see themselves as "already given" just by saying so.
/// </item>
/// </list>
/// The wishlist owner never sees other friends' pending self-claims either
/// (only GiftDtoFactory surfaces them back to the owner, as an id list, to
/// hint who to pick in "chi l'ha regalato?" — never as a confirmation).
/// A unique index on (GiftId, MarkedByUserId) means each person has at most
/// one reception record per gift; marking it "not received" again simply
/// removes the row.
/// </summary>
public class GiftReception
{
    public Guid Id { get; set; }

    public Guid GiftId { get; set; }

    /// <summary>Who recorded this row — the wishlist owner (definitive) or a friend (a private, retractable self-claim).</summary>
    public Guid MarkedByUserId { get; set; }

    /// <summary>
    /// Who actually gave the gift. On the owner's row, this is whoever the
    /// owner picked. On a friend's row, this is always the same as
    /// MarkedByUserId — a friend can only ever claim to have given it themselves.
    /// </summary>
    public Guid GivenByUserId { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Gift Gift { get; set; } = null!;
    public User MarkedByUser { get; set; } = null!;
    public User GivenByUser { get; set; } = null!;
}
