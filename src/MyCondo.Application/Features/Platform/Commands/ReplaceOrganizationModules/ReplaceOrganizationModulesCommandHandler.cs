using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;

/// <summary>
/// Idempotent set-replace over tenancy.tenant_modules — not incremental add/remove. This table
/// carries no RLS (platform-administered metadata about a tenant, not tenant-owned data — same
/// reasoning as tenancy.tenants itself), so the ambient, DI-injected repositories are used directly;
/// no tenant-scoped unit of work is needed here (contrast ProvisionOrganizationWithAdminCommandHandler,
/// which writes to RLS-protected identity.* tables).
/// </summary>
public sealed class ReplaceOrganizationModulesCommandHandler(
    ITenantRepository tenants,
    ITenantModuleRepository tenantModules,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentPlatformUserProvider currentPlatformUser,
    ILogger<ReplaceOrganizationModulesCommandHandler> logger
) : IRequestHandler<ReplaceOrganizationModulesCommand>
{
    public async ValueTask<Unit> Handle(ReplaceOrganizationModulesCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        await tenantModules.ReplaceForTenantAsync(
            tenant.Id.Value, command.ModuleKeys, clock.UtcNow, currentPlatformUser.PlatformUserId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} module set replaced ({ModuleCount} modules enabled)",
            tenant.Id, command.ModuleKeys.Count);
        return Unit.Value;
    }
}
