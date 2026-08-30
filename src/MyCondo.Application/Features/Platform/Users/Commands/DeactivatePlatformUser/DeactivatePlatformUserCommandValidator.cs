using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Commands.DeactivatePlatformUser;

public sealed class DeactivatePlatformUserCommandValidator : AbstractValidator<DeactivatePlatformUserCommand>
{
    public DeactivatePlatformUserCommandValidator()
    {
        RuleFor(x => x.PlatformUserId).NotEmpty();
    }
}
