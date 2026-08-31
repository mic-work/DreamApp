using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.DTOs.Users;

/// <summary>Payload for PUT /api/users/me — editing your own personal data (username, email).</summary>
public class UserUpdateDto
{
    [Required(ErrorMessage = "Lo username è obbligatorio.")]
    [MinLength(3, ErrorMessage = "Lo username deve avere almeno 3 caratteri.")]
    [MaxLength(32, ErrorMessage = "Lo username può avere al massimo 32 caratteri.")]
    [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "Lo username può contenere solo lettere, numeri, punti e underscore.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "L'email non è valida.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
