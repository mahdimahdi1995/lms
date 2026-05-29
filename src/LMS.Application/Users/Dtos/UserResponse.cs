namespace LMS.Application.Users.Dtos;

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? ManagerId,
    bool IsDeleted,
    DateTime CreatedAt);
