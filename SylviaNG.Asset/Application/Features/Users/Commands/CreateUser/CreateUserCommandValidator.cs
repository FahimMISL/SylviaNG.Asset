using FluentValidation;

namespace RMS.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.EmploymentType).IsInEnum().When(c => c.EmploymentType.HasValue);
        RuleFor(c => c.Grade).MaximumLength(100);
        RuleFor(c => c.Designation).MaximumLength(100);
        RuleFor(c => c.Department).MaximumLength(100);
        RuleFor(c => c.Location).MaximumLength(100);
    }
}
