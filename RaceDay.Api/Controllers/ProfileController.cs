using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public ProfileController(AppDbContext db) => _db = db;

    /// <summary>Gets the logged-in user's own profile (Organiser or Participant).</summary>
    /// <remarks>Expects no body. Returns userId, fullName, email and role.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return NotFound(new { message = "User not found." });
        return Ok(new UserProfileDto(user.UserId, user.FullName, user.Email, user.Role));
    }

    /// <summary>Updates the logged-in user's own name and email.</summary>
    /// <remarks>Expects fullName and email. Returns the updated profile. Email must not belong to another account.</remarks>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return NotFound(new { message = "User not found." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
            return Conflict(new { message = "That email is already in use." });

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(new UserProfileDto(user.UserId, user.FullName, user.Email, user.Role));
    }
}