namespace DreamApp.Api.Models;

/// <summary>
/// Restricts a single gift to a specific friend, or designates them as its
/// intended giver. A gift with no GiftVisibility rows is visible to every
/// friend who can already read the wishlist (the default, unrestricted
/// behaviour); a gift with at least one row is visible only to the listed
/// users (in addition to the owner, who always sees their own gifts).
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
