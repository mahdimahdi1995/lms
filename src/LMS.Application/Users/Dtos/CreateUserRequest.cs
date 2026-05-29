namespace LMS.Application.Users.Dtos;

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? ManagerId = null);
