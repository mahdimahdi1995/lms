using LMS.Application.Interfaces;
using LMS.Application.Users.Dtos;
using LMS.Application.Users.Validators;
using LMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    // ── Admin: paged list ────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetUsers(
        [FromServices] IUserService users,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await users.GetUsersAsync(page, pageSize, ct);
        return Ok(result);
    }

    // ── Own profile ──────────────────────────────────────────────────────────────

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(
        [FromServices] IUserService users,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var user = await users.GetMeAsync(currentUser.UserId, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(
        UpdateUserRequest request,
        [FromServices] IUserService users,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] UpdateUserRequestValidator validator,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var (user, error) = await users.UpdateMeAsync(currentUser.UserId, request, ct);
        return user is not null ? Ok(user) : BadRequest(new { error });
    }

    // ── Manager: own team ────────────────────────────────────────────────────────

    [HttpGet("my-team")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> GetMyTeam(
        [FromServices] IUserService users,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var team = await users.GetMyTeamAsync(currentUser.UserId, ct);
        return Ok(team);
    }

    // ── Admin + Manager: get by ID ───────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IUserService users,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        bool isAdmin = currentUser.Role == Roles.Admin;
        var user = await users.GetUserByIdAsync(id, currentUser.UserId, isAdmin, ct);
        return user is null ? NotFound() : Ok(user);
    }

    // ── Admin: create ────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        [FromServices] IUserService users,
        [FromServices] CreateUserRequestValidator validator,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var (user, error) = await users.CreateUserAsync(request, ct);
        return user is not null
            ? CreatedAtAction(nameof(GetById), new { id = user.Id }, user)
            : BadRequest(new { error });
    }

    // ── Admin: update ────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        [FromServices] IUserService users,
        [FromServices] UpdateUserRequestValidator validator,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var (user, error) = await users.UpdateUserAsync(id, request, ct);
        if (error == "User not found.") return NotFound();
        return user is not null ? Ok(user) : BadRequest(new { error });
    }

    // ── Admin: soft-delete ───────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IUserService users,
        CancellationToken ct = default)
    {
        var deleted = await users.DeleteUserAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ── Admin: assign role ───────────────────────────────────────────────────────

    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AssignRole(
        Guid id,
        AssignRoleRequest request,
        [FromServices] IUserService users,
        CancellationToken ct = default)
    {
        if (!Roles.All.Contains(request.Role))
            return BadRequest(new { error = $"Role must be one of: {string.Join(", ", Roles.All)}." });

        var success = await users.AssignRoleAsync(id, request.Role, ct);
        return success ? NoContent() : NotFound();
    }
}
