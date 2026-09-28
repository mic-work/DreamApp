namespace DreamApp.Api.DTOs.Gifts;

/// <summary>
/// Body of PUT /api/gifts/{id}/reception. Meaning of GivenByUserId depends
/// on who's calling — see GiftsController.SetReception / Models/GiftReception.
/// </summary>
public class GiftReceptionRequestDto
{
    public bool Received { get; set; }

    /// <summary>
    /// Required when Received is true AND the caller is the wishlist
    /// owner: who actually gave the gift (must have access to the
    /// wishlist). Ignored when the caller is a friend — a friend can only
    /// ever claim to have given it themselves, so the server always uses
    /// the caller's own id regardless of what's sent here.
    /// </summary>
    public Guid? GivenByUserId { get; set; }
}
