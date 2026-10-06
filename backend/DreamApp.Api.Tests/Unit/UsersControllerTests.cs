using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Users;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

public class UsersControllerTests
{
    [Fact]
    public async Task GetByUsername_ExistingUser_ReturnsPublicInfoWithoutEmail()
    {
        var db = new FakeAppDbContext();
        await new AuthController(db, new PasswordHasher(), new FakeJwtTokenService())
            .Register(new RegisterRequestDto { Username = "tatina", Email = "tatina@example.com", Password = "password123", ConfirmPassword = "password123" });

        var result = new UsersController(db).AsUser(Guid.NewGuid()).GetByUsername("tatina");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserPublicDto>(ok.Value);
        Assert.Equal("tatina", dto.Username);
        Assert.NotEqual(Guid.Empty, dto.WishlistId);
    }

    [Fact]
    public void GetByUsername_UnknownUser_ReturnsNotFound()
    {
        var db = new FakeAppDbContext();

        var result = new UsersController(db).AsUser(Guid.NewGuid()).GetByUsername("ghost");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private static Task RegisterAsync(FakeAppDbContext db, string username) =>
        new AuthController(db, new PasswordHasher(), new FakeJwtTokenService())
            .Register(new RegisterRequestDto { Username = username, Email = $"{username}@example.com", Password = "password123", ConfirmPassword = "password123" });

    [Fact]
    public async Task Search_PartialUsername_ReturnsEveryMatchIgnoringCase()
    {
        var db = new FakeAppDbContext();
        foreach (var name in new[] { "michele", "Gabriele", "tatina", "marco" })
        {
            await RegisterAsync(db, name);
        }

        // "ele" is in the middle/end of two names, "MIC" is a prefix typed in capitals.
        var byEnding = Assert.IsAssignableFrom<IEnumerable<UserPublicDto>>(
            Assert.IsType<OkObjectResult>(new UsersController(db).AsUser(Guid.NewGuid()).Search("ele").Result).Value);
        var byPrefix = Assert.IsAssignableFrom<IEnumerable<UserPublicDto>>(
            Assert.IsType<OkObjectResult>(new UsersController(db).AsUser(Guid.NewGuid()).Search("MIC").Result).Value);

        Assert.Equal(new[] { "Gabriele", "michele" }, byEnding.Select(u => u.Username).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(new[] { "michele" }, byPrefix.Select(u => u.Username));
        Assert.All(byEnding, u => Assert.NotEqual(Guid.Empty, u.WishlistId));
    }

    [Fact]
    public async Task Search_NeverReturnsTheCaller()
    {
        var db = new FakeAppDbContext();
        await RegisterAsync(db, "michele");
        await RegisterAsync(db, "michelangelo");
        var callerId = db.Users.First(u => u.Username == "michele").Id;

        var result = new UsersController(db).AsUser(callerId).Search("mic");

        var users = Assert.IsAssignableFrom<IEnumerable<UserPublicDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new[] { "michelangelo" }, users.Select(u => u.Username));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQuery_ReturnsEmptyList(string? query)
    {
        var db = new FakeAppDbContext();
        await RegisterAsync(db, "michele");

        var result = new UsersController(db).AsUser(Guid.NewGuid()).Search(query);

        var users = Assert.IsAssignableFrom<IEnumerable<UserPublicDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Empty(users);
    }

    [Fact]
    public async Task Search_ManyMatches_AreCappedAtTwenty()
    {
        var db = new FakeAppDbContext();
        for (var i = 0; i < 25; i++)
        {
            await RegisterAsync(db, $"user{i:00}");
        }

        var result = new UsersController(db).AsUser(Guid.NewGuid()).Search("user");

        var users = Assert.IsAssignableFrom<IEnumerable<UserPublicDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(20, users.Count());
    }
}
