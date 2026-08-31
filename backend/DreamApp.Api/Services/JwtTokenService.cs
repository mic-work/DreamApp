using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DreamApp.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace DreamApp.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            // Fail loudly rather than silently signing tokens with a weak/empty key.
            throw new InvalidOperationException(
                "Jwt:Key non è configurato. In sviluppo esegui 'dotnet user-secrets set \"Jwt:Key\" \"<stringa-casuale-lunga>\"' " +
                "dentro backend/DreamApp.Api; in produzione imposta la variabile d'ambiente Jwt__Key.");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "DreamApp";
        var audience = _configuration["Jwt:Audience"] ?? "DreamAppClients";
        var expiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60 * 24 * 7; // 7 days by default: personal app, few users.

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }
}
