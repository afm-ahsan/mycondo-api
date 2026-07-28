using FluentValidation;

namespace MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;

public sealed class GrantPermissionToRoleCommandValidator : AbstractValidator<GrantPermissionToRoleCommand>
{
    public GrantPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}
