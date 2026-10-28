using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class CategoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CategoryTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanAddCategory_ToOwnEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "Under 20", distance = 5, price = 80 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "Under 20", distance = 5, price = 80 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategory_ToAnotherOrganisersEvent()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var other = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var res = await other.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new { categoryName = "Hijack", distance = 5, price = 80 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewCategories()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var list = await participant.GetFromJsonAsync<List<CategoryResponseDto>>($"/api/events/{ev.EventId}/categories");
        Assert.Single(list!);
    }
}