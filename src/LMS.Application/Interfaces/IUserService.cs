using LMS.Application.Common;
using LMS.Application.Users.Dtos;

namespace LMS.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<UserResponse?> GetUserByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<(UserResponse? User, string? Error)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<(UserResponse? User, string? Error)> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<bool> AssignRoleAsync(Guid id, string role, CancellationToken ct = default);
    Task<IReadOnlyList<UserResponse>> GetMyTeamAsync(Guid managerId, CancellationToken ct = default);
    Task<UserResponse?> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<(UserResponse? User, string? Error)> UpdateMeAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
}
