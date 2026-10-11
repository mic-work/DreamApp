using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.DTOs.Wishlists;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

/// <summary>
/// The "N desideri disponibili" shown for each friend on the Home page must
/// match what the caller can really open on that friend's list.
/// </summary>
public class FriendsWishlistsCountTests
{
    private static async Task<(FakeAppDbContext Db, User Alice, User Bob, User Carol)> SeedAsync()
    {
        var db = new FakeAppDbContext();
        var auth = new AuthController(db, new PasswordHasher(), new FakeJwtTokenService());
        foreach (var name in new[] { "alice", "bob", "carol" })
        {
            await auth.Register(new RegisterRequestDto { Username = name, Email = $"{name}@example.com", Password = "password123", ConfirmPassword = "password123" });
        }

        var alice = db.Users.First(u => u.Username == "alice");
        var bob = db.Users.First(u => u.Username == "bob");
        var carol = db.Users.First(u => u.Username == "carol");

        // bob and carol follow alice's list; alice follows both (so she can pick them as recipients).
        var aliceWishlistId = db.Wishlists.First(w => w.OwnerId == alice.Id).Id;
        foreach (var friend in new[] { bob, carol })
        {
            await new WishlistsController(db).AsUser(friend.Id).GrantAccessToSelf(aliceWishlistId);
            var friendWishlistId = db.Wishlists.First(w => w.OwnerId == friend.Id).Id;
            await new WishlistsController(db).AsUser(alice.Id).GrantAccessToSelf(friendWishlistId);
        }

        return (db, alice, bob, carol);
    }

    private static async Task<Guid> CreateGiftAsync(FakeAppDbContext db, Guid ownerId, string name, bool visibleToAll, params Guid[] visibleTo)
    {
        var created = await new GiftsController(db).AsUser(ownerId).Create(new GiftCreateDto
        {
            Name = name,
            Priority = GiftPriority.Medium,
            VisibleToAll = visibleToAll,
            VisibleToUserIds = visibleTo.ToList()
        });
        return ((GiftDto)((CreatedAtActionResult)created.Result!).Value!).Id;
    }

    private static int CountSeenBy(FakeAppDbContext db, Guid viewerId, string ownerUsername)
    {
        var result = new WishlistsController(db).AsUser(viewerId).GetFriendsWishlists();
        var summaries = (List<WishlistSummaryDto>)Assert.IsType<OkObjectResult>(result.Result).Value!;
        return summaries.Single(s => s.OwnerUsername == ownerUsername).GiftsCount;
    }

    [Fact]
    public async Task GiftsCount_IgnoresGiftsHiddenFromTheCaller()
    {
        var (db, alice, bob, carol) = await SeedAsync();
        await CreateGiftAsync(db, alice.Id, "Per tutti", visibleToAll: true);
        await CreateGiftAsync(db, alice.Id, "Solo carol", visibleToAll: false, carol.Id);
        await CreateGiftAsync(db, alice.Id, "Segreto", visibleToAll: false);

        Assert.Equal(1, CountSeenBy(db, bob.Id, "alice"));
        Assert.Equal(2, CountSeenBy(db, carol.Id, "alice"));
    }

    [Fact]
    public async Task GiftsCount_DoesNotCountGiftsTheOwnerConfirmedAsGivenByTheCaller()
    {
        var (db, alice, bob, carol) = await SeedAsync();
        var giftId = await CreateGiftAsync(db, alice.Id, "Cuffie", visibleToAll: true);
        await CreateGiftAsync(db, alice.Id, "Libro", visibleToAll: true);

        await new GiftsController(db).AsUser(alice.Id)
            .SetReception(giftId, new GiftReceptionRequestDto { Received = true, GivenByUserId = bob.Id });

        Assert.Equal(1, CountSeenBy(db, bob.Id, "alice"));
        Assert.Equal(2, CountSeenBy(db, carol.Id, "alice"));
    }
}
