using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api) => _api = api;

    // List only the events this organiser created
    public async Task<IActionResult> Events()
    {
        var result = await _api.GetAsync<List<EventItem>>("api/events");
        if (!result.Success) TempData["Error"] = result.Error;

        var myId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        var mine = (result.Data ?? new List<EventItem>()).Where(e => e.OrganiserId == myId).ToList();
        return View(mine);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        ViewBag.Heading = "Create event";
        return View("EventForm", new EventFormModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventItem>($"api/events/{id}");
        var myId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        if (!result.Success || result.Data == null || result.Data.OrganiserId != myId)
        {
            TempData["Error"] = result.Success ? "You can only edit events you created." : result.Error;
            return RedirectToAction("Events");
        }

        var e = result.Data;
        ViewBag.Heading = "Edit event";
        return View("EventForm", new EventFormModel
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description ?? string.Empty,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType
        });
    }

    // One POST handles both create (EventId == 0) and update
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(EventFormModel model)
    {
        ViewBag.Heading = model.EventId == 0 ? "Create event" : "Edit event";
        if (!ModelState.IsValid) return View("EventForm", model);

        var body = new
        {
            model.EventName,
            model.Description,
            model.EventDate,
            model.Location,
            model.Distance,
            model.EventType
        };

        var result = model.EventId == 0
            ? await _api.PostAsync<EventItem>("api/events", body)
            : await _api.PutAsync<EventItem>($"api/events/{model.EventId}", body);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }

        TempData["Success"] = model.EventId == 0 ? "Event created." : "Event updated.";
        return RedirectToAction("Events");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteAsync<object>($"api/events/{id}");
        if (result.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Error;
        return RedirectToAction("Events");
    }
}