using DreamApp.Api.Models;
using DreamApp.Api.Services;

namespace DreamApp.Api.Tests.Fakes;

/// <summary>Stub used by the fast unit tests, which only care that a token-like value is returned.</summary>
public class FakeJwtTokenService : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) GenerateToken(User user) =>
        ($"fake-token-for-user-{user.Id}", DateTime.UtcNow.AddDays(1));
}
