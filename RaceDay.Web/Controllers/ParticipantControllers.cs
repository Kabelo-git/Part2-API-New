using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api) => _api = api;

    // ---------- event details and enrolment ----------

    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetAsync<EventItem>($"api/events/{id}");
        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction("Index", "Home");
        }

        var cats = await _api.GetAsync<List<CategoryItem>>($"api/events/{id}/categories");
        var mine = await _api.GetAsync<List<EnrolmentItem>>("api/enrolments/mine");

        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = cats.Data ?? new List<CategoryItem>(),
            MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await _api.PostAsync<EnrolmentItem>("api/enrolments", new { categoryId });
        if (result.Success) TempData["Success"] = "You're enrolled! Your enrolment is confirmed.";
        else TempData["Error"] = result.Error;

        return RedirectToAction("EventDetails", new { id = eventId });
    }

    // ---------- my enrolments ----------

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await _api.GetAsync<List<EnrolmentItem>>("api/enrolments/mine");
        if (!result.Success) TempData["Error"] = result.Error;
        return View((result.Data ?? new List<EnrolmentItem>()).OrderByDescending(e => e.EnrolmentDate).ToList());
    }

    // ---------- my results ----------

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<MyResultItem>>("api/results/mine");
        if (!result.Success) TempData["Error"] = result.Error;
        return View((result.Data ?? new List<MyResultItem>()).OrderByDescending(r => r.EventDate).ToList());
    }

    // ---------- profile ----------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _api.GetAsync<UserProfile>("api/profile");
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Role = result.Data.Role;
        return View(new ProfileFormModel { FullName = result.Data.FullName, Email = result.Data.Email });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileFormModel model)
    {
        ViewBag.Role = HttpContext.Session.GetString(SessionKeys.Role);
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PutAsync<UserProfile>("api/profile", new { model.FullName, model.Email });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        if (result.Data != null)
            HttpContext.Session.SetString(SessionKeys.Name, result.Data.FullName);

        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Profile");
    }
}