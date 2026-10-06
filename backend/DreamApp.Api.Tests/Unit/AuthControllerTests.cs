using DreamApp.Api.Controllers;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.Services;
using DreamApp.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DreamApp.Api.Tests.Unit;

public class AuthControllerTests
{
    private static AuthController CreateController(FakeAppDbContext db) =>
        new(db, new PasswordHasher(), new FakeJwtTokenService());

    private static RegisterRequestDto ValidRegistration(string username = "michele", string email = "michele@example.com") => new()
    {
        Username = username,
        Email = email,
        Password = "password123",
        ConfirmPassword = "password123"
    };

    [Fact]
    public async Task Register_CreatesUserAndPersonalWishlist()
    {
        var db = new FakeAppDbContext();
        var controller = CreateController(db);

        var result = await controller.Register(ValidRegistration());

        Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.Single(db.Users);
        Assert.Equal("michele", user.Username);
        Assert.NotEqual("password123", user.PasswordHash); // never stored in clear text
        Assert.Contains(db.Wishlists, w => w.OwnerId == user.Id);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var db = new FakeAppDbContext();
        var controller = CreateController(db);
        await controller.Register(ValidRegistration());

        var result = await CreateController(db).Register(ValidRegistration(email: "other@example.com"));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());

        var result = await CreateController(db).Register(ValidRegistration(username: "someoneelse"));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());

        var result = CreateController(db).Login(new LoginRequestDto { Username = "michele", Password = "password123" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.Token));
        Assert.Equal("michele", body.User.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());

        var result = CreateController(db).Login(new LoginRequestDto { Username = "michele", Password = "wrong-password" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public void Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var db = new FakeAppDbContext();

        var result = CreateController(db).Login(new LoginRequestDto { Username = "ghost", Password = "whatever" });

        // Deliberately the same response as a wrong password, to avoid leaking which usernames exist.
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    private static ChangePasswordRequestDto PasswordChange(string current, string next) => new()
    {
        CurrentPassword = current,
        NewPassword = next,
        ConfirmNewPassword = next
    };

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_StoresTheNewOneAndLoginUsesIt()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());
        var userId = Assert.Single(db.Users).Id;

        var result = await CreateController(db).AsUser(userId).ChangePassword(PasswordChange("password123", "newpassword456"));

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<OkObjectResult>(CreateController(db).Login(new LoginRequestDto { Username = "michele", Password = "newpassword456" }).Result);
        Assert.IsType<UnauthorizedObjectResult>(CreateController(db).Login(new LoginRequestDto { Username = "michele", Password = "password123" }).Result);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequestAndKeepsThePassword()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());
        var userId = Assert.Single(db.Users).Id;

        var result = await CreateController(db).AsUser(userId).ChangePassword(PasswordChange("not-my-password", "newpassword456"));

        // 400 (not 401): a 401 would make the web app log the user out.
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<OkObjectResult>(CreateController(db).Login(new LoginRequestDto { Username = "michele", Password = "password123" }).Result);
    }

    [Fact]
    public async Task ChangePassword_ToTheSamePassword_ReturnsBadRequest()
    {
        var db = new FakeAppDbContext();
        await CreateController(db).Register(ValidRegistration());
        var userId = Assert.Single(db.Users).Id;

        var result = await CreateController(db).AsUser(userId).ChangePassword(PasswordChange("password123", "password123"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ForUnknownUser_ReturnsNotFound()
    {
        var db = new FakeAppDbContext();

        var result = await CreateController(db).AsUser(Guid.NewGuid()).ChangePassword(PasswordChange("password123", "newpassword456"));

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
