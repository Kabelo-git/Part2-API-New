using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? search, string? type, DateTime? dateFrom, DateTime? dateTo)
    {
        var role = HttpContext.Session.GetString(SessionKeys.Role);
        if (string.IsNullOrEmpty(role)) return View("Welcome");
        if (role == "Organiser") return RedirectToAction("Dashboard", "Organiser");

        var result = await _api.GetAsync<List<EventItem>>("api/events");
        if (!result.Success) TempData["Error"] = result.Error;

        var query = (result.Data ?? new List<EventItem>()).Where(e => e.EventDate >= DateTime.Now);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (dateFrom.HasValue)
            query = query.Where(e => e.EventDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(e => e.EventDate.Date <= dateTo.Value.Date);

        return View(new HomeViewModel
        {
            Events = query.OrderBy(e => e.EventDate).ToList(),
            Search = search,
            Type = type,
            DateFrom = dateFrom,
            DateTo = dateTo
        });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}