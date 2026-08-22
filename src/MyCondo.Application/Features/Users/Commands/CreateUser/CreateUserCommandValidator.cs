using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.PhoneNumber).NotEmpty().MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Password).NotEmpty().MustBeAStrongPassword();
    }
}
