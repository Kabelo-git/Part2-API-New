using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[SessionAuthorize]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    private static CategoryResponseDto ToDto(Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.Distance, c.Price);

    /// <summary>Lists the categories of an event (both roles).</summary>
    /// <remarks>Expects the event id in the URL. Returns an array of categories (e.g. Under 20, Senior, 10km).</remarks>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var cats = await _db.Categories.AsNoTracking().Where(c => c.EventId == eventId).ToListAsync();
        return Ok(cats.Select(ToDto));
    }

    /// <summary>Adds a category to one of your events (Organiser only).</summary>
    /// <remarks>Expects the event id in the URL plus categoryName, distance and price. Returns the created category.</remarks>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only add categories to your own events.");

        var cat = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            Distance = dto.Distance,
            Price = dto.Price
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, ToDto(cat));
    }

    /// <summary>Updates a category on one of your events (Organiser only).</summary>
    /// <remarks>Expects the category id in the URL plus categoryName, distance and price. Returns the updated category.</remarks>
    [HttpPut("categories/{categoryId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryDto dto)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat is null) return NotFound(new { message = "Category not found." });
        if (cat.Event.OrganiserId != CurrentUserId) return Forbidden("You can only change categories on your own events.");

        cat.CategoryName = dto.CategoryName.Trim();
        cat.Distance = dto.Distance;
        cat.Price = dto.Price;
        await _db.SaveChangesAsync();
        return Ok(ToDto(cat));
    }

    /// <summary>Deletes a category from one of your events (Organiser only).</summary>
    /// <remarks>Expects the category id in the URL. Returns 204. 409 if participants are already enrolled in it.</remarks>
    [HttpDelete("categories/{categoryId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat is null) return NotFound(new { message = "Category not found." });
        if (cat.Event.OrganiserId != CurrentUserId) return Forbidden("You can only delete categories on your own events.");

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == categoryId))
            return Conflict(new { message = "Participants are enrolled in this category." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}