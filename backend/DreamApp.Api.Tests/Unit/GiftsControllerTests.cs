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

    // ----- Visibility: who can see a gift -----

    /// <summary>
    /// alice (owner) + bob + carol. alice has added bob and carol as friends
    /// (so she can pick them as recipients), and both of them can read alice's wishlist.
    /// </summary>
    private static async Task<(FakeAppDbContext Db, User Alice, User Bob, User Carol, Guid AliceWishlistId)> SeedOwnerWithTwoFriendsAsync()
    {
        var (db, alice, bob) = await SeedTwoUsersAsync();
        var auth = new AuthController(db, new PasswordHasher(), new FakeJwtTokenService());
        await auth.Register(new RegisterRequestDto { Username = "carol", Email = "carol@example.com", Password = "password123", ConfirmPassword = "password123" });
        var carol = db.Users.First(u => u.Username == "carol");

        var aliceWishlistId = db.Wishlists.First(w => w.OwnerId == alice.Id).Id;
        await new WishlistsController(db).AsUser(bob.Id).GrantAccessToSelf(aliceWishlistId);
        await new WishlistsController(db).AsUser(carol.Id).GrantAccessToSelf(aliceWishlistId);
        foreach (var friend in new[] { bob, carol })
        {
            var friendWishlistId = db.Wishlists.First(w => w.OwnerId == friend.Id).Id;
            await new WishlistsController(db).AsUser(alice.Id).GrantAccessToSelf(friendWishlistId);
        }

        return (db, alice, bob, carol, aliceWishlistId);
    }

    private static List<GiftDto> GiftsSeenBy(FakeAppDbContext db, Guid viewerId, Guid wishlistId)
    {
        var result = new GiftsController(db).AsUser(viewerId).GetGifts(wishlistId);
        return (List<GiftDto>)Assert.IsType<OkObjectResult>(result.Result).Value!;
    }

    private static async Task<GiftDto> CreateGiftAsync(FakeAppDbContext db, Guid ownerId, GiftCreateDto dto)
    {
        var created = await new GiftsController(db).AsUser(ownerId).Create(dto);
        return (GiftDto)((CreatedAtActionResult)created.Result!).Value!;
    }

    [Fact]
    public async Task Create_WithoutVisibilityChoice_IsVisibleToEveryFriend()
    {
        var (db, alice, bob, carol, wishlistId) = await SeedOwnerWithTwoFriendsAsync();

        var gift = await CreateGiftAsync(db, alice.Id, new GiftCreateDto { Name = "AirPods", Priority = GiftPriority.High });

        Assert.True(gift.VisibleToAll);
        Assert.Single(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Single(GiftsSeenBy(db, carol.Id, wishlistId));
        Assert.Empty(db.GiftVisibilities);
    }

    [Fact]
    public async Task Create_SharedWithNobody_IsHiddenFromEveryFriendButVisibleToTheOwner()
    {
        var (db, alice, bob, carol, wishlistId) = await SeedOwnerWithTwoFriendsAsync();

        var gift = await CreateGiftAsync(db, alice.Id,
            new GiftCreateDto { Name = "Segreto", Priority = GiftPriority.Low, VisibleToAll = false, VisibleToUserIds = new() });

        Assert.False(gift.VisibleToAll);
        Assert.Empty(gift.VisibleToUserIds);
        Assert.Empty(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Empty(GiftsSeenBy(db, carol.Id, wishlistId));
        Assert.IsType<ForbidResult>(new GiftsController(db).AsUser(bob.Id).GetById(gift.Id).Result);

        // The owner still sees it in their own list.
        var ownerList = (List<GiftDto>)Assert.IsType<OkObjectResult>(new GiftsController(db).AsUser(alice.Id).GetGifts(null).Result).Value!;
        Assert.Single(ownerList);
    }

    [Fact]
    public async Task Create_SharedWithSelectedFriends_IsVisibleOnlyToThem()
    {
        var (db, alice, bob, carol, wishlistId) = await SeedOwnerWithTwoFriendsAsync();

        var gift = await CreateGiftAsync(db, alice.Id,
            new GiftCreateDto { Name = "Libro", Priority = GiftPriority.Medium, VisibleToAll = false, VisibleToUserIds = new() { bob.Id } });

        Assert.False(gift.VisibleToAll);
        Assert.Equal(new[] { bob.Id }, gift.VisibleToUserIds);
        Assert.Single(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Empty(GiftsSeenBy(db, carol.Id, wishlistId));
        Assert.IsType<OkObjectResult>(new GiftsController(db).AsUser(bob.Id).GetById(gift.Id).Result);
        Assert.IsType<ForbidResult>(new GiftsController(db).AsUser(carol.Id).GetById(gift.Id).Result);
    }

    [Fact]
    public async Task Create_SharedWithEveryone_IgnoresTheListedFriendsAndStoresNoRows()
    {
        var (db, alice, bob, carol, wishlistId) = await SeedOwnerWithTwoFriendsAsync();

        var gift = await CreateGiftAsync(db, alice.Id,
            new GiftCreateDto { Name = "Scarpe", Priority = GiftPriority.Medium, VisibleToAll = true, VisibleToUserIds = new() { bob.Id } });

        Assert.True(gift.VisibleToAll);
        Assert.Empty(gift.VisibleToUserIds);
        Assert.Empty(db.GiftVisibilities);
        Assert.Single(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Single(GiftsSeenBy(db, carol.Id, wishlistId));
    }

    [Fact]
    public async Task Update_CanSwitchBetweenEveryoneNobodyAndSelectedFriends()
    {
        var (db, alice, bob, carol, wishlistId) = await SeedOwnerWithTwoFriendsAsync();
        var gift = await CreateGiftAsync(db, alice.Id, new GiftCreateDto { Name = "Zaino", Priority = GiftPriority.Medium });

        // everyone -> nobody
        await new GiftsController(db).AsUser(alice.Id).Update(gift.Id,
            new GiftUpdateDto { Name = "Zaino", Priority = GiftPriority.Medium, VisibleToAll = false });
        Assert.Empty(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Empty(GiftsSeenBy(db, carol.Id, wishlistId));

        // nobody -> only carol
        await new GiftsController(db).AsUser(alice.Id).Update(gift.Id,
            new GiftUpdateDto { Name = "Zaino", Priority = GiftPriority.Medium, VisibleToAll = false, VisibleToUserIds = new() { carol.Id } });
        Assert.Empty(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Single(GiftsSeenBy(db, carol.Id, wishlistId));

        // only carol -> everyone (old rows are cleaned up)
        await new GiftsController(db).AsUser(alice.Id).Update(gift.Id,
            new GiftUpdateDto { Name = "Zaino", Priority = GiftPriority.Medium, VisibleToAll = true });
        Assert.Single(GiftsSeenBy(db, bob.Id, wishlistId));
        Assert.Single(GiftsSeenBy(db, carol.Id, wishlistId));
        Assert.Empty(db.GiftVisibilities);
    }

    [Fact]
    public async Task Create_SharedWithPeopleWhoAreNotMyFriends_IgnoresThemAndHidesTheGift()
    {
        var (db, alice, bob, _, wishlistId) = await SeedOwnerWithTwoFriendsAsync();

        var gift = await CreateGiftAsync(db, alice.Id,
            new GiftCreateDto { Name = "Cuffie", Priority = GiftPriority.Low, VisibleToAll = false, VisibleToUserIds = new() { Guid.NewGuid() } });

        Assert.Empty(gift.VisibleToUserIds);
        Assert.Empty(GiftsSeenBy(db, bob.Id, wishlistId));
    }
}
