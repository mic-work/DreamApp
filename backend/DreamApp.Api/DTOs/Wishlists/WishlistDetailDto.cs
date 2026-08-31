using DreamApp.Api.DTOs.Gifts;

namespace DreamApp.Api.DTOs.Wishlists;

/// <summary>Full wishlist content, including gifts.</summary>
public class WishlistDetailDto
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>True when the caller owns this wishlist and can therefore edit/delete its gifts.</summary>
    public bool IsOwner { get; set; }

    public List<GiftDto> Gifts { get; set; } = new();
}
