using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.Models;

/// <summary>A single gift/desire inside a wishlist.</summary>
public class Gift
{
    public int Id { get; set; }

    [Required]
    public int WishlistId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Relative or absolute URL to the gift's photo. Never a raw binary blob:
    /// keeping only a URL here is what makes it trivial to swap the local
    /// upload implementation (see Services/LocalImageStorageService) for a
    /// cloud storage provider later without touching the data model.
    /// </summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    [MaxLength(2048)]
    public string? ProductUrl { get; set; }

    public GiftPriority Priority { get; set; } = GiftPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Wishlist Wishlist { get; set; } = null!;
}
