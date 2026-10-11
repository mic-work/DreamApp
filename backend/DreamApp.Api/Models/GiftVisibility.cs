namespace DreamApp.Api.Models;

/// <summary>
/// Lets a specific friend see a single gift, or designates them as its
/// intended giver. Rows only matter when Gift.VisibleToAll is false: in that
/// case the gift is visible only to the listed users (in addition to the
/// owner, who always sees their own gifts), so a gift with
/// VisibleToAll = false and no rows is visible to nobody but the owner.
/// When Gift.VisibleToAll is true every friend who can read the wishlist
/// sees the gift and there are no rows.
/// UserId is always someone the OWNER has personally added as a friend
/// (i.e. whose wishlist the owner can see) — never someone who merely added
/// the owner. This is deliberate: it lets the owner say "this one is for
/// Michele" even before Michele has added them back, and it means a friend
/// who only added the owner (without being added back) never gets to see a
/// gift restricted to someone else.
/// </summary>
public class GiftVisibility
{
    public Guid Id { get; set; }

    public Guid GiftId { get; set; }
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Gift Gift { get; set; } = null!;
    public User User { get; set; } = null!;
}
