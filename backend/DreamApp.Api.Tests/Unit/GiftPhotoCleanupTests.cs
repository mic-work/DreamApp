using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

/// <summary>Photo files must not pile up on disk once no gift uses them any more.</summary>
public class GiftPhotoCleanupTests
{
    private static async Task<(FakeAppDbContext Db, User Alice, FakeImageStorageService Images)> SeedAsync()
    {
        var db = new FakeAppDbContext();
        var auth = new AuthController(db, new PasswordHasher(), new FakeJwtTokenService());
        await auth.Register(new RegisterRequestDto { Username = "alice", Email = "alice@example.com", Password = "password123", ConfirmPassword = "password123" });
        return (db, db.Users.First(u => u.Username == "alice"), new FakeImageStorageService());
    }

    private static async Task<Guid> CreateGiftWithPhotosAsync(FakeAppDbContext db, FakeImageStorageService images, Guid ownerId, params string[] photos)
    {
        var created = await new GiftsController(db, images).AsUser(ownerId)
            .Create(new GiftCreateDto { Name = "Cuffie", Priority = GiftPriority.High, ImageUrls = photos.ToList() });
        return ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;
    }

    [Fact]
    public async Task Delete_RemovesThePhotoFilesOfTheGift()
    {
        var (db, alice, images) = await SeedAsync();
        var giftId = await CreateGiftWithPhotosAsync(db, images, alice.Id, "/uploads/a.png", "/uploads/b.png");

        await new GiftsController(db, images).AsUser(alice.Id).Delete(giftId);

        Assert.Equal(new[] { "/uploads/a.png", "/uploads/b.png" }, images.Deleted.OrderBy(u => u).ToArray());
    }

    [Fact]
    public async Task Update_RemovesOnlyThePhotosTheOwnerDropped()
    {
        var (db, alice, images) = await SeedAsync();
        var giftId = await CreateGiftWithPhotosAsync(db, images, alice.Id, "/uploads/a.png", "/uploads/b.png");

        await new GiftsController(db, images).AsUser(alice.Id).Update(giftId,
            new GiftUpdateDto { Name = "Cuffie", Priority = GiftPriority.High, ImageUrls = new() { "/uploads/a.png" } });

        Assert.Equal(new[] { "/uploads/b.png" }, images.Deleted.ToArray());
    }

    [Fact]
    public async Task Delete_ByANonOwner_KeepsThePhotos()
    {
        var (db, alice, images) = await SeedAsync();
        var giftId = await CreateGiftWithPhotosAsync(db, images, alice.Id, "/uploads/a.png");

        var result = await new GiftsController(db, images).AsUser(Guid.NewGuid()).Delete(giftId);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(images.Deleted);
    }

    [Fact]
    public void LocalStorage_Delete_RemovesOnlyFilesInsideTheImagesFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dreamapp-cleanup-{Guid.NewGuid():N}");
        var imagesDir = Path.Combine(root, "uploads");
        Directory.CreateDirectory(imagesDir);
        var inside = Path.Combine(imagesDir, "photo.png");
        var outside = Path.Combine(root, "secret.txt");
        File.WriteAllText(inside, "x");
        File.WriteAllText(outside, "x");

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:ImagesPath"] = imagesDir })
                .Build();
            var storage = new LocalImageStorageService(config);

            storage.Delete("/uploads/../secret.txt"); // path traversal attempt
            storage.Delete("https://example.com/uploads/photo.png"); // not one of our URLs
            storage.Delete("");
            Assert.True(File.Exists(inside));
            Assert.True(File.Exists(outside));

            storage.Delete("/uploads/photo.png");
            Assert.False(File.Exists(inside));
            Assert.True(File.Exists(outside));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
