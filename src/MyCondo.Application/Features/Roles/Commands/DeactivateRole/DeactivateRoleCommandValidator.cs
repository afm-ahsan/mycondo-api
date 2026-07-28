using FluentValidation;

namespace MyCondo.Application.Features.Roles.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandValidator : AbstractValidator<DeactivateRoleCommand>
{
    public DeactivateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
