using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Commands.ActivatePlatformUser;

public sealed class ActivatePlatformUserCommandValidator : AbstractValidator<ActivatePlatformUserCommand>
{
    public ActivatePlatformUserCommandValidator()
    {
        RuleFor(x => x.PlatformUserId).NotEmpty();
    }
}
