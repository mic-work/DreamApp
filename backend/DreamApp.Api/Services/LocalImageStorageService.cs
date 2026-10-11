using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DreamApp.Api.Services;

/// <summary>
/// Stores uploaded gift photos on the backend's local disk, under the same
/// configurable, persistent data directory as the SQLite database (see
/// IMAGES_PATH / Storage:ImagesPath). Files are served back via
/// app.UseStaticFiles() in Program.cs at the "/uploads" URL prefix.
/// </summary>
public class LocalImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB: plenty for a phone photo, keeps the disk usage sane.

    private readonly string _imagesPath;
    private readonly string _publicBasePath;

    public LocalImageStorageService(IConfiguration configuration)
    {
        _imagesPath = ResolveImagesPath(configuration);
        Directory.CreateDirectory(_imagesPath);
        _publicBasePath = "/uploads";
    }

    public static string ResolveImagesPath(IConfiguration configuration)
    {
        var configured = Environment.GetEnvironmentVariable("IMAGES_PATH")
                          ?? configuration["Storage:ImagesPath"]
                          ?? "App_Data/uploads";
        return Path.GetFullPath(configured);
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new ArgumentException("Il file caricato è vuoto.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Il file supera la dimensione massima consentita (5 MB).");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Formato immagine non supportato. Usa JPG, PNG, GIF o WEBP.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_imagesPath, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"{_publicBasePath}/{fileName}";
    }

    public void Delete(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(_publicBasePath + "/", StringComparison.Ordinal))
        {
            return;
        }

        // Path.GetFileName drops any "../" segments, so only files directly inside the images folder can ever be removed.
        var fileName = Path.GetFileName(imageUrl);
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        try
        {
            var fullPath = Path.Combine(_imagesPath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException)
        {
            // Best effort: a photo that can't be removed now is just left on disk.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
