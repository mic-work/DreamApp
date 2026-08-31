using System.ComponentModel.DataAnnotations;
using DreamApp.Api.Models;

namespace DreamApp.Api.DTOs.Gifts;

public class GiftUpdateDto
{
    [Required(ErrorMessage = "Il nome del regalo è obbligatorio.")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    [MaxLength(2048)]
    [Url(ErrorMessage = "Il link al prodotto non è un URL valido.")]
    public string? ProductUrl { get; set; }

    public GiftPriority Priority { get; set; } = GiftPriority.Medium;
}
