using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
[SessionAuthorize]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    private static EventResponseDto ToDto(Event e) => new(e.EventId, e.OrganiserId, e.EventName,
        e.Description, e.EventDate, e.Location, e.Distance, e.EventType);

    /// <summary>Lists all events (both roles).</summary>
    /// <remarks>Expects no body. Returns an array of events ordered by date.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Gets one event by its id (both roles).</summary>
    /// <remarks>Expects the event id in the URL. Returns the event, or 404 if it does not exist.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? NotFound(new { message = "Event not found." }) : Ok(ToDto(ev));
    }

    /// <summary>Creates an event (Organiser only).</summary>
    /// <remarks>Expects eventName, description, eventDate, location, distance and eventType (run, walk or cycle). Returns the created event.</remarks>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventDto dto)
    {
        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = dto.EventName.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            Distance = dto.Distance,
            EventType = dto.EventType.ToLowerInvariant()
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>Updates an event you created (Organiser only).</summary>
    /// <remarks>Expects the event id in the URL and the same fields as create. Returns the updated event. 403 if the event belongs to another organiser.</remarks>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only change events you created.");

        ev.EventName = dto.EventName.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location.Trim();
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType.ToLowerInvariant();
        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes an event you created (Organiser only).</summary>
    /// <remarks>Expects the event id in the URL. Returns 204 on success. 409 if participants have already enrolled.</remarks>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only delete events you created.");

        if (await _db.Enrolments.AnyAsync(e => e.Category.EventId == id))
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}