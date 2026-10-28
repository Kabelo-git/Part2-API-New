using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ResultTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, HttpClient participant, EnrolmentResponseDto enrolment)> SetupAsync()
    {
        var organiser = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var cat = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        var res = await participant.PostAsJsonAsync("/api/enrolments", new { categoryId = cat.CategoryId });
        var enrolment = (await res.Content.ReadFromJsonAsync<EnrolmentResponseDto>())!;
        return (organiser, participant, enrolment);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_ReturnsForbidden()
    {
        var (_, participant, enrolment) = await SetupAsync();
        var res = await participant.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentId, finishTime = "00:50:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForAnotherOrganisersEvent()
    {
        var (_, _, enrolment) = await SetupAsync();
        var stranger = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var res = await stranger.PostAsJsonAsync("/api/results",
            new { enrolmentId = enrolment.EnrolmentId, finishTime = "00:50:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CapturingResultTwice_ReturnsConflict()
    {
        var (organiser, _, enrolment) = await SetupAsync();
        var body = new { enrolmentId = enrolment.EnrolmentId, finishTime = "00:50:00", position = 1 };

        await organiser.PostAsJsonAsync("/api/results", body);
        var second = await organiser.PostAsJsonAsync("/api/results", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}