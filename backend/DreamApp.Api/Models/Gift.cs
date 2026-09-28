using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.Models;

/// <summary>A single gift/desire inside a wishlist.</summary>
public class Gift
{
    public Guid Id { get; set; }

    [Required]
    public Guid WishlistId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Deprecated single-photo field, kept only so gifts saved before the
    /// multi-photo gallery (see <see cref="Photos"/>) don't lose their
    /// picture: it is no longer written to by new code (Photos is now the
    /// source of truth), and is read only as a fallback when a gift has no
    /// GiftPhoto rows yet. Never a raw binary blob: keeping only a URL here
    /// is what makes it trivial to swap the local upload implementation
    /// (see Services/LocalImageStorageService) for a cloud storage provider
    /// later without touching the data model.
    /// </summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    [MaxLength(2048)]
    public string? ProductUrl { get; set; }

    public GiftPriority Priority { get; set; } = GiftPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Wishlist Wishlist { get; set; } = null!;

    /// <summary>Photo gallery, ordered by GiftPhoto.SortOrder.</summary>
    public List<GiftPhoto> Photos { get; set; } = new();

    /// <summary>When empty, every friend with access to the wishlist can see this gift. When not empty, only the listed users can (plus the owner).</summary>
    public List<GiftVisibility> VisibleTo { get; set; } = new();

    /// <summary>Per-friend "already given" records — see GiftReception for why these are private.</summary>
    public List<GiftReception> Receptions { get; set; } = new();
}
