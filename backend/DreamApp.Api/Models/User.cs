using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.Models;

/// <summary>
/// An application user (one of the two people using DreamApp).
/// Passwords are never stored in clear text, only their PBKDF2 hash.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    [Required, MaxLength(32)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by <see cref="Services.IPasswordHasher"/>. Never the raw password.</summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<Wishlist> Wishlists { get; set; } = new();
    public List<WishlistAccess> WishlistAccesses { get; set; } = new();
}
