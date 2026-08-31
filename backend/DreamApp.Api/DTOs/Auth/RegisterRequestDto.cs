using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.DTOs.Auth;

public class RegisterRequestDto
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

    [Required(ErrorMessage = "La password è obbligatoria.")]
    [MinLength(6, ErrorMessage = "La password deve avere almeno 6 caratteri.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La conferma password è obbligatoria.")]
    [Compare(nameof(Password), ErrorMessage = "Le password non coincidono.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
