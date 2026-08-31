namespace DreamApp.Api.DTOs.Wishlists;

/// <summary>Lightweight summary used in lists (Home page, "friends" list).</summary>
public class WishlistSummaryDto
{
    public int Id { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int GiftsCount { get; set; }
}
