using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateRoleCommandHandler> logger
) : IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    public async ValueTask<CreateRoleResult> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string name = command.Name.Trim();

        Role? existing = await roles.GetByNameAsync(tenantId, name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A role named '{name}' already exists for this tenant.");
        }

        Role role = Role.CreateCustom(tenantId, name, command.Description, clock.UtcNow);

        roles.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Role {RoleId} '{Name}' created for tenant {TenantId}", role.Id, role.Name, tenantId);

        return new CreateRoleResult(role.Id.Value, role.Name, role.Description);
    }
}
