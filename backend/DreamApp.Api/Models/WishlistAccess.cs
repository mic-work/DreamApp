namespace DreamApp.Api.Models;

/// <summary>
/// Grants <see cref="UserId"/> read-only access to <see cref="WishlistId"/>,
/// which belongs to a different user. This is how "adding a friend's list"
/// is represented: no data is copied, just a permission row. Removing this
/// row ("rimuovi amico") never touches the owner's wishlist or gifts.
/// A unique index on (UserId, WishlistId) prevents duplicates.
/// </summary>
public class WishlistAccess
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid WishlistId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Wishlist Wishlist { get; set; } = null!;
}
