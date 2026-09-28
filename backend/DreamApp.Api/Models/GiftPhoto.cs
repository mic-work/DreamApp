namespace DreamApp.Api.Models;

/// <summary>
/// One photo attached to a gift. A gift can have several photos, shown in
/// <see cref="SortOrder"/> order in the gallery. Kept as its own table
/// (rather than a comma-separated column) so photos can be added, removed
/// and reordered independently of the gift row itself.
/// </summary>
public class GiftPhoto
{
    public Guid Id { get; set; }

    public Guid GiftId { get; set; }

    /// <summary>Relative or absolute URL returned by POST /api/images. Never a raw binary blob (see Gift.ImageUrl).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>0-based display order within the gift's gallery.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Gift Gift { get; set; } = null!;
}
