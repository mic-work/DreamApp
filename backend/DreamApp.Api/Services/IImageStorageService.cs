using Microsoft.AspNetCore.Http;

namespace DreamApp.Api.Services;

/// <summary>
/// Abstraction over "where gift photos are stored". The first implementation
/// (<see cref="LocalImageStorageService"/>) writes to local disk, which is
/// fine for a personal app with two users, but any other implementation
/// (S3, Azure Blob Storage, Cloudinary, ...) can be swapped in later by
/// registering it in Program.cs — no controller or database change needed,
/// because Gift.ImageUrl only ever stores a URL, never binary data.
/// </summary>
public interface IImageStorageService
{
    /// <summary>Saves the uploaded file and returns the URL clients should use to display it.</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously saved photo given the URL returned by <see cref="SaveAsync"/>.
    /// Best effort: an unknown/foreign URL or a missing file is silently ignored.
    /// </summary>
    void Delete(string imageUrl);
}
