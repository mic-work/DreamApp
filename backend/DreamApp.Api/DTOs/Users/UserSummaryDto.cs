namespace DreamApp.Api.DTOs.Users;

/// <summary>Minimal public info about a friend, used to populate pickers (gift visibility, "who gave it") — never includes the email.</summary>
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
}
