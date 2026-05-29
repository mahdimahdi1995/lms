using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LMS.Application.Users.Dtos;
using LMS.Domain.Constants;
using LMS.Infrastructure.Persistence;
using LMS.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.IntegrationTests.Users;

public class UserEndpointTests : IClassFixture<LmsWebApplicationFactory>, IAsyncLifetime
{
    private const string TestPassword = "Password1!";
    private const string AdminEmail = "admin@test.com";
    private const string ManagerEmail = "manager@test.com";
    private const string LearnerEmail = "learner@test.com";

    private static class Routes
    {
        public const string Login = "/api/auth/login";
        public const string Users = "/api/users";
        public const string Me = "/api/users/me";
        public const string MyTeam = "/api/users/my-team";
        public static string ById(Guid id) => $"/api/users/{id}";
        public static string Role(Guid id) => $"/api/users/{id}/role";
    }

    private readonly LmsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private string _adminToken = "";
    private string _managerToken = "";
    private string _learnerToken = "";
    private Guid _managerId;
    private Guid _learnerId;
    private Guid _adminId;

    public UserEndpointTests(LmsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUserRoles");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers");

        var admin = new ApplicationUser
        {
            UserName = AdminEmail, Email = AdminEmail,
            FirstName = "Admin", LastName = "User"
        };
        await userManager.CreateAsync(admin, TestPassword);
        await userManager.AddToRoleAsync(admin, Roles.Admin);
        _adminId = admin.Id;

        var manager = new ApplicationUser
        {
            UserName = ManagerEmail, Email = ManagerEmail,
            FirstName = "Manager", LastName = "User"
        };
        await userManager.CreateAsync(manager, TestPassword);
        await userManager.AddToRoleAsync(manager, Roles.Manager);
        _managerId = manager.Id;

        var learner = new ApplicationUser
        {
            UserName = LearnerEmail, Email = LearnerEmail,
            FirstName = "Learner", LastName = "User",
            ManagerId = manager.Id
        };
        await userManager.CreateAsync(learner, TestPassword);
        await userManager.AddToRoleAsync(learner, Roles.Learner);
        _learnerId = learner.Id;

        _adminToken = await LoginAsync(AdminEmail);
        _managerToken = await LoginAsync(ManagerEmail);
        _learnerToken = await LoginAsync(LearnerEmail);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/users ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsPaged200()
    {
        using var client = AuthClient(_adminToken);
        var response = await client.GetAsync(Routes.Users);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UserResponse>>();
        body!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Learner)]
    public async Task GetUsers_NonAdmin_Returns403(string role)
    {
        using var client = AuthClient(GetToken(role));
        var response = await client.GetAsync(Routes.Users);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/users/me ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ReturnsOwnProfile()
    {
        using var client = AuthClient(_learnerToken);
        var response = await client.GetAsync(Routes.Me);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Id.Should().Be(_learnerId);
        body.Role.Should().Be(Roles.Learner);
    }

    // ── PUT /api/users/me ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMe_ReturnsUpdatedProfile()
    {
        using var client = AuthClient(_learnerToken);
        var response = await client.PutAsJsonAsync(Routes.Me, new UpdateUserRequest("Updated", "Name"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.FirstName.Should().Be("Updated");
        body.LastName.Should().Be("Name");
    }

    // ── GET /api/users/my-team ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyTeam_AsManager_ReturnsOnlyOwnTeam()
    {
        using var client = AuthClient(_managerToken);
        var response = await client.GetAsync(Routes.MyTeam);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        body!.Should().HaveCount(1);
        body[0].Id.Should().Be(_learnerId);
    }

    [Fact]
    public async Task GetMyTeam_AsLearner_Returns403()
    {
        using var client = AuthClient(_learnerToken);
        var response = await client.GetAsync(Routes.MyTeam);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_AsAdmin_ReturnsAnyUser()
    {
        using var client = AuthClient(_adminToken);
        var response = await client.GetAsync(Routes.ById(_learnerId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Id.Should().Be(_learnerId);
    }

    [Fact]
    public async Task GetById_AsManager_OwnTeamMember_Returns200()
    {
        using var client = AuthClient(_managerToken);
        var response = await client.GetAsync(Routes.ById(_learnerId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_AsManager_OtherUser_Returns404()
    {
        using var client = AuthClient(_managerToken);
        var response = await client.GetAsync(Routes.ById(_adminId));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/users ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_AsAdmin_Returns201()
    {
        using var client = AuthClient(_adminToken);
        var response = await client.PostAsJsonAsync(Routes.Users, new CreateUserRequest(
            "newuser@test.com", TestPassword, "New", "User", Roles.Trainer));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Role.Should().Be(Roles.Trainer);
    }

    [Theory]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Learner)]
    public async Task CreateUser_NonAdmin_Returns403(string role)
    {
        using var client = AuthClient(GetToken(role));
        var response = await client.PostAsJsonAsync(Routes.Users, new CreateUserRequest(
            "extra@test.com", TestPassword, "Extra", "User", Roles.Learner));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PUT /api/users/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_AsAdmin_Returns200()
    {
        using var client = AuthClient(_adminToken);
        var response = await client.PutAsJsonAsync(Routes.ById(_learnerId),
            new UpdateUserRequest("EditedFirst", "EditedLast"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.FirstName.Should().Be("EditedFirst");
    }

    // ── DELETE /api/users/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_AsAdmin_SoftDeletesUser()
    {
        using var client = AuthClient(_adminToken);
        var deleteResponse = await client.DeleteAsync(Routes.ById(_learnerId));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync(Routes.Users);
        var body = await listResponse.Content.ReadFromJsonAsync<PagedResult<UserResponse>>();
        body!.Items.Should().NotContain(u => u.Id == _learnerId);
    }

    // ── PUT /api/users/{id}/role ──────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_AsAdmin_ChangesRole()
    {
        using var client = AuthClient(_adminToken);
        var assignResponse = await client.PutAsJsonAsync(Routes.Role(_learnerId),
            new AssignRoleRequest(Roles.Trainer));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync(Routes.ById(_learnerId));
        var body = await getResponse.Content.ReadFromJsonAsync<UserResponse>();
        body!.Role.Should().Be(Roles.Trainer);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private string GetToken(string role) => role switch
    {
        Roles.Admin => _adminToken,
        Roles.Manager => _managerToken,
        _ => _learnerToken
    };

    private async Task<string> LoginAsync(string email)
    {
        var resp = await _client.PostAsJsonAsync(Routes.Login,
            new { Email = email, Password = TestPassword });
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.Token;
    }

    private HttpClient AuthClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record TokenResponse(string Token);
    private record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
}
