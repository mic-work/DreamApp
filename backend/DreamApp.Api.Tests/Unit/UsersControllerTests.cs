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
}
