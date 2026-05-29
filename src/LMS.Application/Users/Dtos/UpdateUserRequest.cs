namespace LMS.Application.Users.Dtos;

public record UpdateUserRequest(string FirstName, string LastName, Guid? ManagerId = null);
