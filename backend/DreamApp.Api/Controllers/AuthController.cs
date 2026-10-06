using DreamApp.Api.Common;
using DreamApp.Api.Data;
using DreamApp.Api.DTOs.Auth;
using DreamApp.Api.DTOs.Users;
using DreamApp.Api.Models;
using DreamApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DreamApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string DefaultWishlistName = "La mia lista";

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IAppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
    {
        var normalizedUsername = dto.Username.Trim();
        var normalizedEmail = dto.Email.Trim();

        if (_db.Users.Any(u => u.Username.ToLower() == normalizedUsername.ToLower()))
        {
            return Conflict(new ApiErrorDto("Questo username è già in uso."));
        }

        if (_db.Users.Any(u => u.Email.ToLower() == normalizedEmail.ToLower()))
        {
            return Conflict(new ApiErrorDto("Questa email è già registrata."));
        }

        var user = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };
        _db.Add(user);

        // Every user gets exactly one personal wishlist, created automatically.
        var wishlist = new Wishlist
        {
            Owner = user,
            Name = DefaultWishlistName,
            CreatedAt = DateTime.UtcNow
        };
        _db.Add(wishlist);

        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);
        return CreatedAtAction(nameof(GetCurrentUser), null, BuildAuthResponse(user, token, expiresAt));
    }

    [HttpPost("login")]
    public ActionResult<AuthResponseDto> Login(LoginRequestDto dto)
    {
        var username = dto.Username.Trim();
        var user = _db.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());

        // Deliberately generic message: don't reveal whether the username exists.
        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new ApiErrorDto("Username o password non corretti."));
        }

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);
        return Ok(BuildAuthResponse(user, token, expiresAt));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserDto> GetCurrentUser()
    {
        var userId = User.GetUserId();
        var user = _db.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        return Ok(ToUserDto(user));
    }

    /// <summary>
    /// Lets the logged-in user change their own password. The target is always
    /// the caller (taken from the JWT, never from the client) and the CURRENT
    /// password must be provided, so a stolen/left-open session alone can't
    /// lock the owner out.
    ///
    /// A wrong current password is answered with 400, not 401: a 401 would make
    /// the web app treat the session as expired and log the user out.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto dto)
    {
        var userId = User.GetUserId();
        var user = _db.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(new ApiErrorDto("Utente non trovato."));
        }

        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new ApiErrorDto("La password attuale non è corretta."));
        }

        if (dto.NewPassword == dto.CurrentPassword)
        {
            return BadRequest(new ApiErrorDto("La nuova password deve essere diversa da quella attuale."));
        }

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AuthResponseDto BuildAuthResponse(User user, string token, DateTime expiresAt) => new()
    {
        Token = token,
        ExpiresAt = expiresAt,
        User = ToUserDto(user)
    };

    private static UserDto ToUserDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    };
}
