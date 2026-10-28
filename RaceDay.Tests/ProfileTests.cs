using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class ProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ProfileTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetProfile_WhenLoggedIn_ReturnsOwnProfile()
    {
        var client = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/profile");
        Assert.NotNull(profile);
        Assert.Equal("Participant", profile!.Role);
    }

    [Fact]
    public async Task GetProfile_WithoutLogin_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var client = await TestHelpers.LoginAsNewUserAsync(_factory, "Organiser");
        var res = await client.PutAsJsonAsync("/api/profile",
            new { fullName = "New Name", email = TestHelpers.NewEmail() });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/profile");
        Assert.Equal("New Name", profile!.FullName);
    }

    [Fact]
    public async Task UpdateProfile_WithEmailOfAnotherUser_ReturnsConflict()
    {
        var takenEmail = TestHelpers.NewEmail();
        var other = _factory.CreateClient();
        await other.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Other", email = takenEmail, password = TestHelpers.Password, role = "Participant" });

        var client = await TestHelpers.LoginAsNewUserAsync(_factory, "Participant");
        var res = await client.PutAsJsonAsync("/api/profile", new { fullName = "X", email = takenEmail });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }
}
