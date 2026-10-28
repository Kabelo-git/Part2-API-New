using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public EnrolmentTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponseDto>>("/api/enrolments/mine");
        Assert.NotNull(mine);
        var recorded = Assert.Single(mine!);
        Assert.Equal(ev.EventId, recorded.EventId);
        Assert.Equal(cat.CategoryId, recorded.CategoryId);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_ReturnsForbidden()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var res = await organiser.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent_ReturnsConflict()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });
        var second = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });

        var list = await organiser.GetFromJsonAsync<List<EnrolmentResponseDto>>($"/api/events/{ev.EventId}/enrolments");
        Assert.Single(list!);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_ReturnsForbidden()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var res = await participant.GetAsync($"/api/events/{ev.EventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var enrolRes = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });
        var enrolment = await enrolRes.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

        var capture = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment!.EnrolmentId, finishTime = "00:48:30", position = 3 });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<ResultResponseDto>>("/api/results/mine");
        Assert.Equal(3, Assert.Single(mine!).Position);
    }
}