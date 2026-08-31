namespace DreamApp.Api.Models;

/// <summary>
/// Priority level assigned by the wishlist owner to a gift/desire.
/// Stored in the database as a string (see AppDbContext configuration)
/// so the raw SQLite data stays human readable.
/// </summary>
public enum GiftPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
