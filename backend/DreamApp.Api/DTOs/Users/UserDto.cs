namespace DreamApp.Api.DTOs.Users;

/// <summary>Full profile of the currently authenticated user (returned to that user only).</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
