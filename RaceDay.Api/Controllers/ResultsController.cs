using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[SessionAuthorize]
public class ResultsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) => _db = db;

    /// <summary>Captures a participant's finish time and position (Organiser only).</summary>
    /// <remarks>Expects enrolmentId, finishTime (hh:mm:ss, e.g. 01:45:30) and position. Returns the saved result. 403 if the enrolment is for another organiser's event. 409 if a result already exists.</remarks>
    [HttpPost("results")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture([FromBody] CreateResultDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category.Event.OrganiserId != CurrentUserId)
            return Forbidden("You can only capture results for your own events.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result { EnrolmentId = dto.EnrolmentId, FinishTime = dto.FinishTime, Position = dto.Position };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new ResultResponseDto(result.ResultId,
            enrolment.EnrolmentId, enrolment.ParticipantId, enrolment.Category.EventId,
            enrolment.Category.Event.EventName, result.FinishTime, result.Position));
    }

    /// <summary>Lists your own results (Participant only).</summary>
    /// <remarks>Expects no body. Returns your finish times and positions with the event names.</remarks>
    [HttpGet("results/mine")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .Select(r => new ResultResponseDto(r.ResultId, r.EnrolmentId, r.Enrolment.ParticipantId,
                r.Enrolment.Category.EventId, r.Enrolment.Category.Event.EventName, r.FinishTime, r.Position))
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all results for one of your events (Organiser only).</summary>
    /// <remarks>Expects the event id in the URL. Returns every captured result for that event.</remarks>
    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view results for your own events.");

        var list = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .Select(r => new ResultResponseDto(r.ResultId, r.EnrolmentId, r.Enrolment.ParticipantId,
                r.Enrolment.Category.EventId, r.Enrolment.Category.Event.EventName, r.FinishTime, r.Position))
            .ToListAsync();
        return Ok(list);
    }
}
