using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;

public sealed class RevokePlatformRoleFromUserCommandValidator : AbstractValidator<RevokePlatformRoleFromUserCommand>
{
    public RevokePlatformRoleFromUserCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PlatformUserId).NotEmpty();
    }
}
