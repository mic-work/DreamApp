using System.ComponentModel.DataAnnotations;
using DreamApp.Api.Common;
using DreamApp.Api.Models;

namespace DreamApp.Api.DTOs.Gifts;

public class GiftCreateDto
{
    [Required(ErrorMessage = "Il nome del regalo è obbligatorio.")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Deprecated: new gifts should use <see cref="ImageUrls"/> instead.</summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    /// <summary>Photo URLs (already uploaded via POST /api/images), in display order.</summary>
    public List<string> ImageUrls { get; set; } = new();

    [MaxLength(2048)]
    [OptionalUrl(ErrorMessage = "Il link al prodotto non è un URL valido.")]
    public string? ProductUrl { get; set; }

    public GiftPriority Priority { get; set; } = GiftPriority.Medium;

    /// <summary>Friend ids this gift should be restricted to. Empty/omitted = visible to every friend who can see the wishlist.</summary>
    public List<Guid> VisibleToUserIds { get; set; } = new();
}
