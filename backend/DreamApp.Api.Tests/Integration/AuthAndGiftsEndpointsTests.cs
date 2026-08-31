using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Gifts;
using DreamApp.Api.Models;
using Xunit;

namespace DreamApp.Api.Tests.Integration;

/// <summary>
/// End-to-end tests through the real HTTP pipeline: real JWT issuance and
/// validation, real EF Core + SQLite persistence. Each test uses its own
/// CustomWebApplicationFactory so every test gets a clean database file.
/// </summary>
public class AuthAndGiftsEndpointsTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthAndGiftsEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<AuthResponseDto> RegisterAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    [Fact]
    public async Task Register_ReturnsJwtToken_UsableForAuthenticatedRequests()
    {
        var auth = await RegisterAsync("michele-" + Guid.NewGuid().ToString("N")[..8]);

        Assert.False(string.IsNullOrEmpty(auth.Token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var me = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/wishlists/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var username = "login-test-" + Guid.NewGuid().ToString("N")[..8];
        await RegisterAsync(username);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Username = username,
            Password = "not-the-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGift_ThenGetMyWishlist_IncludesTheNewGift()
    {
        var auth = await RegisterAsync("gifter-" + Guid.NewGuid().ToString("N")[..8]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var createResponse = await _client.PostAsJsonAsync("/api/gifts", new GiftCreateDto
        {
            Name = "AirPods",
            Priority = GiftPriority.High
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var wishlistResponse = await _client.GetAsync("/api/wishlists/me");
        wishlistResponse.EnsureSuccessStatusCode();
        var wishlist = await wishlistResponse.Content.ReadFromJsonAsync<DreamApp.Api.DTOs.Wishlists.WishlistDetailDto>();

        Assert.Contains(wishlist!.Gifts, g => g.Name == "AirPods");
    }

    [Fact]
    public async Task UpdateGift_AsAnotherUser_ReturnsForbidden()
    {
        var owner = await RegisterAsync("owner-" + Guid.NewGuid().ToString("N")[..8]);
        var intruder = await RegisterAsync("intruder-" + Guid.NewGuid().ToString("N")[..8]);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        var created = await _client.PostAsJsonAsync("/api/gifts", new GiftCreateDto { Name = "Profumo", Priority = GiftPriority.Medium });
        var gift = await created.Content.ReadFromJsonAsync<GiftDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", intruder.Token);
        var updateResponse = await _client.PutAsJsonAsync($"/api/gifts/{gift!.Id}", new GiftUpdateDto { Name = "Hacked", Priority = GiftPriority.Low });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
