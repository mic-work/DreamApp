using System.Net;
using System.Net.Http.Json;
using DreamApp.Api.DTOs.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DreamApp.Api.Tests.Integration;

public class RateLimitingTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Login_AfterTooManyAttempts_ReturnsTooManyRequestsWithAMessage()
    {
        using var limited = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:AuthPermitLimit"] = "3" })));
        using var client = limited.CreateClient();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "nobody", Password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = "nobody", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Contains("Troppi tentativi", await blocked.Content.ReadAsStringAsync());
    }
}
