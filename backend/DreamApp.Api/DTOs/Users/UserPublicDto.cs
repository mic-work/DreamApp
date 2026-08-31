namespace DreamApp.Api.DTOs.Users;

/// <summary>
/// Public information about a user, exposed to OTHER users during friend
/// search. Deliberately excludes the email address for privacy.
/// </summary>
public class UserPublicDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>Id of this user's personal wishlist, used to request access to it.</summary>
    public int WishlistId { get; set; }
}
