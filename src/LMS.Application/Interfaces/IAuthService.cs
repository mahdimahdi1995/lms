using LMS.Application.Auth.Dtos;

namespace LMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public record AuthResult(bool Succeeded, string? Token, string? Error);
