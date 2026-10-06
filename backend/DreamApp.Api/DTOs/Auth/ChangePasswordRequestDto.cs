using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.DTOs.Auth;

/// <summary>Payload for POST /api/auth/change-password — a logged-in user choosing a new password.</summary>
public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "La password attuale è obbligatoria.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nuova password è obbligatoria.")]
    [MinLength(6, ErrorMessage = "La nuova password deve avere almeno 6 caratteri.")]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La conferma password è obbligatoria.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Le password non coincidono.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
