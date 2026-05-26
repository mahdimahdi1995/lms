using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using LMS.Application.Auth.Dtos;
using LMS.Infrastructure.Persistence;
using LMS.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.IntegrationTests.Auth;

public class AuthEndpointTests : IClassFixture<LmsWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LmsWebApplicationFactory _factory;

    public AuthEndpointTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Runs before each test — ensures the schema exists and wipes user rows
    // so every test starts with an empty slate.
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "alice@example.com", "Password1!", "Alice", "Smith"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var request = new RegisterRequest("bob@example.com", "Password1!", "Bob", "Jones");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("not-an-email", "Password1!", "Test", "User"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("short@example.com", "short", "Test", "User"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login@example.com", "Password1!", "Login", "User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login@example.com", "Password1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("wrongpw@example.com", "Password1!", "Wrong", "Pw"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("wrongpw@example.com", "WrongPass99!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonexistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("ghost@example.com", "Password1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Token contents ────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Token_ContainsLearnerRoleClaim()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("role@example.com", "Password1!", "Role", "Test"));

        var body = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body!.Token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Learner");
    }

    // ── /me endpoint ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_WithValidToken_Returns200WithUserId()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("me@example.com", "Password1!", "Me", "User"));
        var body = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.Token);
        var meResponse = await _client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TokenResponse(string Token);
}
