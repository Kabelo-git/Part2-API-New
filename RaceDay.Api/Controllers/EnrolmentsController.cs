using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[SessionAuthorize]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EnrolmentsController(AppDbContext db) => _db = db;

    private static IQueryable<EnrolmentResponseDto> Project(IQueryable<Enrolment> query) =>
        query.Select(e => new EnrolmentResponseDto(e.EnrolmentId, e.ParticipantId, e.Participant.FullName,
            e.Category.EventId, e.Category.Event.EventName, e.CategoryId, e.Category.CategoryName,
            e.EnrolmentDate, e.Status));

    /// <summary>Enters an event by choosing a category (Participant only).</summary>
    /// <remarks>Expects categoryId. The category identifies the event. Records the link between you, the event and the category, and returns the enrolment. 409 if you are already enrolled in that event.</remarks>
    [HttpPost("enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol([FromBody] EnrolDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (category.Event.EventDate < DateTime.UtcNow)
            return BadRequest(new { message = "This event has already taken place." });

        var alreadyEnrolled = await _db.Enrolments.AnyAsync(e =>
            e.ParticipantId == CurrentUserId && e.Category.EventId == category.EventId);
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed"
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var response = await Project(_db.Enrolments.Where(e => e.EnrolmentId == enrolment.EnrolmentId)).FirstAsync();
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Lists your own enrolments (Participant only).</summary>
    /// <remarks>Expects no body. Returns each enrolment with the event and category you chose.</remarks>
    [HttpGet("enrolments/mine")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var list = await Project(_db.Enrolments.AsNoTracking().Where(e => e.ParticipantId == CurrentUserId)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all enrolments for one of your events (Organiser only).</summary>
    /// <remarks>Expects the event id in the URL. Returns every participant's enrolment for that event. 403 if the event is not yours.</remarks>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var list = await Project(_db.Enrolments.AsNoTracking().Where(e => e.Category.EventId == eventId)).ToListAsync();
        return Ok(list);
    }
}