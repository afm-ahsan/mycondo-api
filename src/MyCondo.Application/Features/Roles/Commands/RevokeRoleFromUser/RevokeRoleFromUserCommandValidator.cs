using FluentValidation;

namespace MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandValidator : AbstractValidator<RevokeRoleFromUserCommand>
{
    public RevokeRoleFromUserCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
