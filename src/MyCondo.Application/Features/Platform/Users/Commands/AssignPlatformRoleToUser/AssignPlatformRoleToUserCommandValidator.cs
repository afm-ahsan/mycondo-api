using FluentValidation;

namespace MyCondo.Application.Features.Platform.Users.Commands.AssignPlatformRoleToUser;

public sealed class AssignPlatformRoleToUserCommandValidator : AbstractValidator<AssignPlatformRoleToUserCommand>
{
    public AssignPlatformRoleToUserCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PlatformUserId).NotEmpty();
    }
}
