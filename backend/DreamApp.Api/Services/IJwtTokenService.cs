using DreamApp.Api.Models;

namespace DreamApp.Api.Services;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT for the given user.</summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
