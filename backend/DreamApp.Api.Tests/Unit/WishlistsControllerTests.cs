using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Wishlists;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

public class WishlistsControllerTests
{
    private static async Task<(FakeAppDbContext Db, User Carol, User Dave)> SeedTwoUsersAsync()
    {
        var db = new FakeAppDbContext();
        var auth = new AuthController(db, new PasswordHasher(), new FakeJwtTokenService());

        await auth.Register(new RegisterRequestDto { Username = "carol", Email = "carol@example.com", Password = "password123", ConfirmPassword = "password123" });
        await auth.Register(new RegisterRequestDto { Username = "dave", Email = "dave@example.com", Password = "password123", ConfirmPassword = "password123" });

        return (db, db.Users.First(u => u.Username == "carol"), db.Users.First(u => u.Username == "dave"));
    }

    [Fact]
    public async Task GetByUsername_WithoutAccess_ReturnsForbidden()
    {
        var (db, carol, dave) = await SeedTwoUsersAsync();

        var result = new WishlistsController(db).AsUser(dave.Id).GetByUsername("carol");

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GrantAccess_ThenRead_ReturnsReadOnlyWishlist()
    {
        var (db, carol, dave) = await SeedTwoUsersAsync();
        var carolWishlistId = db.Wishlists.First(w => w.OwnerId == carol.Id).Id;

        var grant = await new WishlistsController(db).AsUser(dave.Id).GrantAccessToSelf(carolWishlistId);
        Assert.IsType<NoContentResult>(grant);

        var result = new WishlistsController(db).AsUser(dave.Id).GetByUsername("carol");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<WishlistDetailDto>(ok.Value);
        Assert.False(detail.IsOwner);
    }

    [Fact]
    public async Task GrantAccess_ToOwnWishlist_ReturnsBadRequest()
    {
        var (db, carol, _) = await SeedTwoUsersAsync();
        var carolWishlistId = db.Wishlists.First(w => w.OwnerId == carol.Id).Id;

        var result = await new WishlistsController(db).AsUser(carol.Id).GrantAccessToSelf(carolWishlistId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GrantAccess_Twice_ReturnsConflict()
    {
        var (db, carol, dave) = await SeedTwoUsersAsync();
        var carolWishlistId = db.Wishlists.First(w => w.OwnerId == carol.Id).Id;
        await new WishlistsController(db).AsUser(dave.Id).GrantAccessToSelf(carolWishlistId);

        var result = await new WishlistsController(db).AsUser(dave.Id).GrantAccessToSelf(carolWishlistId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task RevokeAccess_RemovesGrant_ButNeverTouchesTheOwnersWishlist()
    {
        var (db, carol, dave) = await SeedTwoUsersAsync();
        var carolWishlistId = db.Wishlists.First(w => w.OwnerId == carol.Id).Id;
        await new WishlistsController(db).AsUser(dave.Id).GrantAccessToSelf(carolWishlistId);

        var revoke = await new WishlistsController(db).AsUser(dave.Id).RevokeAccessToSelf(carolWishlistId);

        Assert.IsType<NoContentResult>(revoke);
        Assert.True(db.Wishlists.Any(w => w.Id == carolWishlistId)); // owner's wishlist untouched
        Assert.False(db.WishlistAccesses.Any(a => a.WishlistId == carolWishlistId && a.UserId == dave.Id));

        var readAfterRevoke = new WishlistsController(db).AsUser(dave.Id).GetByUsername("carol");
        Assert.IsType<ForbidResult>(readAfterRevoke.Result);
    }

    [Fact]
    public async Task GetMyWishlist_IsAlwaysMarkedAsOwner()
    {
        var (db, carol, _) = await SeedTwoUsersAsync();

        var result = new WishlistsController(db).AsUser(carol.Id).GetMyWishlist();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(((WishlistDetailDto)ok.Value!).IsOwner);
    }
}
