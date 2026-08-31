using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

public class GiftsControllerTests
{
    private static async Task<(FakeAppDbContext Db, User Alice, User Bob)> SeedTwoUsersAsync()
    {
        var db = new FakeAppDbContext();
        var auth = new AuthController(db, new PasswordHasher(), new FakeJwtTokenService());

        await auth.Register(new RegisterRequestDto { Username = "alice", Email = "alice@example.com", Password = "password123", ConfirmPassword = "password123" });
        await auth.Register(new RegisterRequestDto { Username = "bob", Email = "bob@example.com", Password = "password123", ConfirmPassword = "password123" });

        return (db, db.Users.First(u => u.Username == "alice"), db.Users.First(u => u.Username == "bob"));
    }

    [Fact]
    public async Task Create_AddsGiftToCallersOwnWishlist()
    {
        var (db, alice, _) = await SeedTwoUsersAsync();
        var controller = new GiftsController(db).AsUser(alice.Id);

        var result = await controller.Create(new GiftCreateDto { Name = "AirPods", Priority = GiftPriority.High });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<GiftDto>(created.Value);
        var aliceWishlistId = db.Wishlists.First(w => w.OwnerId == alice.Id).Id;
        Assert.Equal(aliceWishlistId, dto.WishlistId);
    }

    [Fact]
    public async Task Update_ByNonOwner_ReturnsForbiddenAndDoesNotChangeTheGift()
    {
        var (db, alice, bob) = await SeedTwoUsersAsync();
        var created = await new GiftsController(db).AsUser(alice.Id).Create(new GiftCreateDto { Name = "AirPods", Priority = GiftPriority.High });
        var giftId = ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await new GiftsController(db).AsUser(bob.Id)
            .Update(giftId, new GiftUpdateDto { Name = "Hacked by bob", Priority = GiftPriority.Low });

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal("AirPods", db.Gifts.Single(g => g.Id == giftId).Name);
    }

    [Fact]
    public async Task Delete_ByNonOwner_ReturnsForbiddenAndKeepsTheGift()
    {
        var (db, alice, bob) = await SeedTwoUsersAsync();
        var created = await new GiftsController(db).AsUser(alice.Id).Create(new GiftCreateDto { Name = "Scarpe", Priority = GiftPriority.Medium });
        var giftId = ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await new GiftsController(db).AsUser(bob.Id).Delete(giftId);

        Assert.IsType<ForbidResult>(result);
        Assert.True(db.Gifts.Any(g => g.Id == giftId));
    }

    [Fact]
    public async Task Update_ByOwner_PersistsChanges()
    {
        var (db, alice, _) = await SeedTwoUsersAsync();
        var created = await new GiftsController(db).AsUser(alice.Id).Create(new GiftCreateDto { Name = "AirPods", Priority = GiftPriority.High });
        var giftId = ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await new GiftsController(db).AsUser(alice.Id)
            .Update(giftId, new GiftUpdateDto { Name = "AirPods Pro", Priority = GiftPriority.Medium });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("AirPods Pro", ((GiftDto)ok.Value!).Name);
        Assert.Equal("AirPods Pro", db.Gifts.Single(g => g.Id == giftId).Name);
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesTheGift()
    {
        var (db, alice, _) = await SeedTwoUsersAsync();
        var created = await new GiftsController(db).AsUser(alice.Id).Create(new GiftCreateDto { Name = "Libro", Priority = GiftPriority.Low });
        var giftId = ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await new GiftsController(db).AsUser(alice.Id).Delete(giftId);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.Gifts.Any(g => g.Id == giftId));
    }

    [Fact]
    public async Task GetGifts_ForSomeoneElsesWishlistWithoutAccess_ReturnsForbidden()
    {
        var (db, alice, bob) = await SeedTwoUsersAsync();
        var aliceWishlistId = db.Wishlists.First(w => w.OwnerId == alice.Id).Id;

        var result = new GiftsController(db).AsUser(bob.Id).GetGifts(aliceWishlistId);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
