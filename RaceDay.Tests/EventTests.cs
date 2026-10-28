using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public EventTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var res = await participant.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var res = await participant.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad",
            description = "x",
            eventDate = DateTime.UtcNow.AddDays(5),
            location = "X",
            distance = 5,
            eventType = "swim"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", new
        {
            eventName = "Renamed",
            description = "x",
            eventDate = DateTime.UtcNow.AddDays(40),
            location = "Pretoria",
            distance = 21,
            eventType = "run"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var other = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var res = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", TestHelpers.ValidEvent());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_ReturnsForbidden()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await participant.DeleteAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
