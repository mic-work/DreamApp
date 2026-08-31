using System.ComponentModel.DataAnnotations;

namespace DreamApp.Api.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Lo username è obbligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria.")]
    public string Password { get; set; } = string.Empty;
}
