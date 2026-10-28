using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_WithValidDetails_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane", email = TestHelpers.NewEmail(), password = TestHelpers.Password, role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.NewEmail();
        var body = new { fullName = "Jane", email, password = TestHelpers.Password, role = "Participant" };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Hacker", email = TestHelpers.NewEmail(), password = TestHelpers.Password, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane", email, password = TestHelpers.Password, role = "Organiser" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Jane", email, password = TestHelpers.Password, role = "Organiser" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}