using FluentValidation;
using LMS.Application.Users.Dtos;
using LMS.Domain.Constants;

namespace LMS.Application.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => Roles.All.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");
    }
}
