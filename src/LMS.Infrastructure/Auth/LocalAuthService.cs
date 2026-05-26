using LMS.Application.Auth.Dtos;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace LMS.Infrastructure.Auth;

public class LocalAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;

    public LocalAuthService(UserManager<ApplicationUser> userManager, JwtService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return new AuthResult(false, null, string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "Learner");

        var token = await _jwtService.GenerateTokenAsync(user);
        return new AuthResult(true, token, null);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
            return new AuthResult(false, null, "Invalid credentials.");

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return new AuthResult(false, null, "Invalid credentials.");

        var token = await _jwtService.GenerateTokenAsync(user);
        return new AuthResult(true, token, null);
    }
}
