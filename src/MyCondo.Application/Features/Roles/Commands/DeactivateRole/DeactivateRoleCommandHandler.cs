using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandHandler(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeactivateRoleCommandHandler> logger
) : IRequestHandler<DeactivateRoleCommand>
{
    public async ValueTask<Unit> Handle(DeactivateRoleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        RoleId roleId = new(command.RoleId);
        Role role = await roles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), command.RoleId);

        if (role.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Role), command.RoleId);
        }

        if (role.IsSystem)
        {
            throw new ForbiddenException($"Cannot deactivate system role '{role.Name}'.");
        }

        role.Deactivate(clock.UtcNow, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Role {RoleId} deactivated for tenant {TenantId}", roleId, tenantId);

        return Unit.Value;
    }
}
