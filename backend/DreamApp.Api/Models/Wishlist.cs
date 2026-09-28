using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.Models;

/// <summary>
/// A wishlist owned by a user. In this first version every user has exactly
/// one personal wishlist, created automatically at registration time
/// (see AuthController.Register). The model itself does not enforce a
/// single-wishlist limit, so a future version could allow more than one
/// without a data migration.
/// </summary>
public class Wishlist
{
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerId { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public List<Gift> Gifts { get; set; } = new();

    /// <summary>Other users who have been granted read access to this wishlist.</summary>
    public List<WishlistAccess> AccessGrants { get; set; } = new();
}
