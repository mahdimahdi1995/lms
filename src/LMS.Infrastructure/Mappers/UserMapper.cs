using LMS.Application.Users.Dtos;
using LMS.Infrastructure.Persistence;
using Riok.Mapperly.Abstractions;

namespace LMS.Infrastructure.Mappers;

[Mapper]
public static partial class UserMapper
{
    [MapperIgnoreSource(nameof(ApplicationUser.UserName))]
    [MapperIgnoreSource(nameof(ApplicationUser.NormalizedUserName))]
    [MapperIgnoreSource(nameof(ApplicationUser.NormalizedEmail))]
    [MapperIgnoreSource(nameof(ApplicationUser.EmailConfirmed))]
    [MapperIgnoreSource(nameof(ApplicationUser.PasswordHash))]
    [MapperIgnoreSource(nameof(ApplicationUser.SecurityStamp))]
    [MapperIgnoreSource(nameof(ApplicationUser.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(ApplicationUser.PhoneNumber))]
    [MapperIgnoreSource(nameof(ApplicationUser.PhoneNumberConfirmed))]
    [MapperIgnoreSource(nameof(ApplicationUser.TwoFactorEnabled))]
    [MapperIgnoreSource(nameof(ApplicationUser.LockoutEnd))]
    [MapperIgnoreSource(nameof(ApplicationUser.LockoutEnabled))]
    [MapperIgnoreSource(nameof(ApplicationUser.AccessFailedCount))]
    [MapperIgnoreSource(nameof(ApplicationUser.UpdatedAt))]
    public static partial UserResponse ToResponse(ApplicationUser user, string role);
}
