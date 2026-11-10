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

    // ---------- helpers ----------

    private async Task<List<EventItem>> MyEventsAsync()
    {
        var result = await _api.GetAsync<List<EventItem>>("api/events");
        if (!result.Success) TempData["Error"] = result.Error;

        var myId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        return (result.Data ?? new List<EventItem>()).Where(e => e.OrganiserId == myId).ToList();
    }

    private async Task<EventItem?> GetMyEventAsync(int eventId)
    {
        var result = await _api.GetAsync<EventItem>($"api/events/{eventId}");
        var myId = HttpContext.Session.GetInt32(SessionKeys.UserId);

        if (!result.Success || result.Data == null || result.Data.OrganiserId != myId)
        {
            TempData["Error"] = result.Success ? "You can only manage your own events." : result.Error;
            return null;
        }
        return result.Data;
    }

    // ---------- dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var events = await MyEventsAsync();
        var rows = new List<DashboardRow>();

        foreach (var e in events)
        {
            var enr = await _api.GetAsync<List<EnrolmentItem>>($"api/events/{e.EventId}/enrolments");
            rows.Add(new DashboardRow { Event = e, Enrolments = enr.Data?.Count ?? 0 });
        }

        return View(new DashboardViewModel { Rows = rows.OrderBy(r => r.Event.EventDate).ToList() });
    }

    // ---------- events ----------

    public async Task<IActionResult> Events()
    {
        var mine = await MyEventsAsync();
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
        var e = await GetMyEventAsync(id);
        if (e == null) return RedirectToAction("Events");

        ViewBag.Heading = "Edit event";
        return View("EventForm", new EventFormModel
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType
        });
    }

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

    // ---------- categories ----------

    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await GetMyEventAsync(eventId);
        if (ev == null) return RedirectToAction("Events");

        var cats = await _api.GetAsync<List<CategoryItem>>($"api/events/{eventId}/categories");
        if (!cats.Success) TempData["Error"] = cats.Error;

        return View(new CategoriesViewModel
        {
            Event = ev,
            Categories = cats.Data ?? new List<CategoryItem>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction("Categories", new { eventId = model.EventId });
        }

        var result = await _api.PostAsync<CategoryItem>($"api/events/{model.EventId}/categories",
            new { model.CategoryName, model.Distance, model.Price });

        if (result.Success) TempData["Success"] = "Category added.";
        else TempData["Error"] = result.Error;

        return RedirectToAction("Categories", new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId, int eventId)
    {
        var result = await _api.DeleteAsync<object>($"api/categories/{categoryId}");
        if (result.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = result.Error;

        return RedirectToAction("Categories", new { eventId });
    }

    // ---------- enrolments ----------

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await GetMyEventAsync(eventId);
        if (ev == null) return RedirectToAction("Events");

        var enr = await _api.GetAsync<List<EnrolmentItem>>($"api/events/{eventId}/enrolments");
        if (!enr.Success) TempData["Error"] = enr.Error;

        return View(new EnrolmentsViewModel
        {
            Event = ev,
            Enrolments = enr.Data ?? new List<EnrolmentItem>()
        });
    }

    // ---------- results ----------

    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await GetMyEventAsync(eventId);
        if (ev == null) return RedirectToAction("Events");

        var enr = await _api.GetAsync<List<EnrolmentItem>>($"api/events/{eventId}/enrolments");
        var res = await _api.GetAsync<List<ResultItem>>($"api/events/{eventId}/results");
        if (!enr.Success) TempData["Error"] = enr.Error;
        else if (!res.Success) TempData["Error"] = res.Error;

        return View(new ResultsViewModel
        {
            Event = ev,
            Enrolments = enr.Data ?? new List<EnrolmentItem>(),
            Results = res.Data ?? new List<ResultItem>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultFormModel model)
    {
        if (!ModelState.IsValid || !TimeSpan.TryParse(model.FinishTime, out var time))
        {
            TempData["Error"] = "Enter a finish time like 01:45:30 and a position of 1 or more.";
            return RedirectToAction("Results", new { eventId = model.EventId });
        }

        var result = await _api.PostAsync<ResultItem>("api/results", new
        {
            model.EnrolmentId,
            FinishTime = time.ToString(@"hh\:mm\:ss"),
            model.Position
        });

        if (result.Success) TempData["Success"] = "Result saved.";
        else TempData["Error"] = result.Error;

        return RedirectToAction("Results", new { eventId = model.EventId });
    }
}