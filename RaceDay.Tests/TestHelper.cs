using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Password123!";

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    // Registers a brand-new user, logs in, and returns a client that holds the session cookie
    public static async Task<HttpClient> LoginAsNewUserAsync(CustomWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email, password = Password, role });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object ValidEvent() => new
    {
        eventName = "Soweto 10km",
        description = "Fun run",
        eventDate = DateTime.UtcNow.AddDays(30),
        location = "Soweto",
        distance = 10,
        eventType = "run"
    };

    public static async Task<EventResponseDto> CreateEventAsync(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", ValidEvent());
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<EventResponseDto>())!;
    }

    public static async Task<CategoryResponseDto> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Senior 10km", distance = 10, price = 150 });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CategoryResponseDto>())!;
    }
}