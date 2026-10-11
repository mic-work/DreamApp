using DreamApp.Api.Services;
using Microsoft.AspNetCore.Http;

namespace DreamApp.Api.Tests.Fakes;

/// <summary>Records which photo URLs the controllers asked to delete; never touches the disk.</summary>
public class FakeImageStorageService : IImageStorageService
{
    public List<string> Deleted { get; } = new();

    public Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult($"/uploads/{Guid.NewGuid():N}.png");

    public void Delete(string imageUrl) => Deleted.Add(imageUrl);
}
