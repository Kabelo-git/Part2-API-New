using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.Role)))
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<UserProfile>("api/auth/login", new { model.Email, model.Password });
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);
        HttpContext.Session.SetString(SessionKeys.Name, result.Data.FullName);
        HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<UserProfile>("api/auth/register",
            new { model.FullName, model.Email, model.Password, model.Role });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout", null);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}