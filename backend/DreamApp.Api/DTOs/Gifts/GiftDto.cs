using DreamApp.Api.Models;

namespace DreamApp.Api.DTOs.Gifts;

public class GiftDto
{
    public int Id { get; set; }
    public int WishlistId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public GiftPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>True when the caller owns this gift (i.e. it's in their own wishlist).</summary>
    public bool IsOwner { get; set; }
}
