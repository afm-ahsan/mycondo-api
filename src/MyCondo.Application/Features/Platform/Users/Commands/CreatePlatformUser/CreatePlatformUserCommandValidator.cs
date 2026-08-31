using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;

public sealed class CreatePlatformUserCommandValidator : AbstractValidator<CreatePlatformUserCommand>
{
    public CreatePlatformUserCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MustBeAStrongPassword();
        RuleFor(x => x.InitialRoleName).MaximumLength(100);
    }
}
