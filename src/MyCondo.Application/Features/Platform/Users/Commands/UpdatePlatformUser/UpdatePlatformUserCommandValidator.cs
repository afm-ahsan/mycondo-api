using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Commands.UpdatePlatformUser;

public sealed class UpdatePlatformUserCommandValidator : AbstractValidator<UpdatePlatformUserCommand>
{
    public UpdatePlatformUserCommandValidator()
    {
        RuleFor(x => x.PlatformUserId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
