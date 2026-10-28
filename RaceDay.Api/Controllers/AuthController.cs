using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>Expects fullName, email, password (min 8 characters) and role ("Organiser" or "Participant"). Returns the new user's profile (never the password).</remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var role = Roles.All.FirstOrDefault(r => r.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null)
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new UserProfileDto(user.UserId, user.FullName, user.Email, user.Role));
    }

    /// <summary>Logs in and starts a session that remembers who you are and your role.</summary>
    /// <remarks>Expects email and password. Returns the user's profile. A session cookie is set and used automatically on later requests.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(new UserProfileDto(user.UserId, user.FullName, user.Email, user.Role));
    }

    /// <summary>Logs out and clears the session.</summary>
    /// <remarks>Expects no body. Returns a confirmation message. Requires a logged-in user.</remarks>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
