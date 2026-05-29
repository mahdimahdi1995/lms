using LMS.Application.Common;
using LMS.Application.Interfaces;
using LMS.Application.Users.Dtos;
using LMS.Domain.Constants;
using LMS.Infrastructure.Mappers;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Users;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LmsDbContext _db;

    public UserService(UserManager<ApplicationUser> userManager, LmsDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking();
        var total = await query.CountAsync(ct);
        var users = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = new List<UserResponse>(users.Count);
        foreach (var u in users)
            responses.Add(UserMapper.ToResponse(u, await GetPrimaryRoleAsync(u)));

        return new PagedResult<UserResponse>(responses, total, page, pageSize);
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;
        if (!isAdmin && user.ManagerId != requestingUserId) return null;

        return UserMapper.ToResponse(user, await GetPrimaryRoleAsync(user));
    }

    public async Task<(UserResponse? User, string? Error)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ManagerId = request.ManagerId,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (null, string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);
        return (UserMapper.ToResponse(user, request.Role), null);
    }

    public async Task<(UserResponse? User, string? Error)> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return (null, "User not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.ManagerId = request.ManagerId;
        user.UpdatedAt = DateTime.UtcNow;

        return await SaveUserAsync(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return false;

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> AssignRoleAsync(Guid id, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
        return true;
    }

    public async Task<IReadOnlyList<UserResponse>> GetMyTeamAsync(Guid managerId, CancellationToken ct = default)
    {
        var team = await _db.Users.AsNoTracking()
            .Where(u => u.ManagerId == managerId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .ToListAsync(ct);

        var responses = new List<UserResponse>(team.Count);
        foreach (var u in team)
            responses.Add(UserMapper.ToResponse(u, await GetPrimaryRoleAsync(u)));

        return responses;
    }

    public async Task<UserResponse?> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        return UserMapper.ToResponse(user, await GetPrimaryRoleAsync(user));
    }

    public async Task<(UserResponse? User, string? Error)> UpdateMeAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return (null, "User not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UpdatedAt = DateTime.UtcNow;

        return await SaveUserAsync(user);
    }

    private async Task<string> GetPrimaryRoleAsync(ApplicationUser user) =>
        (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Learner;

    private async Task<(UserResponse? User, string? Error)> SaveUserAsync(ApplicationUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (null, string.Join("; ", result.Errors.Select(e => e.Description)));

        return (UserMapper.ToResponse(user, await GetPrimaryRoleAsync(user)), null);
    }
}
