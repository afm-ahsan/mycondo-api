using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQueryHandler(
    ITenantRepository tenants,
    ITenantModuleRepository tenantModules
) : IRequestHandler<GetOrganizationByIdQuery, OrganizationDetailDto>
{
    public async ValueTask<OrganizationDetailDto> Handle(
        GetOrganizationByIdQuery query, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(query.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), query.OrganizationId);

        List<TenantModule> modules = await tenantModules.GetEnabledForTenantAsync(tenant.Id.Value, cancellationToken);

        OrganizationAdministratorDto? administrator = tenant.PrimaryAdministratorUserId is Guid adminUserId
            ? new OrganizationAdministratorDto(
                adminUserId, tenant.PrimaryAdministratorFullName!, tenant.PrimaryAdministratorEmail!)
            : null;

        return new OrganizationDetailDto(
            TenantId: tenant.Id.Value,
            Name: tenant.Name,
            Code: tenant.Code,
            Slug: tenant.Slug,
            Status: tenant.Status.ToString(),
            CreatedAtUtc: tenant.CreatedAtUtc,
            UpdatedAtUtc: tenant.UpdatedAtUtc,
            Administrator: administrator,
            EnabledModuleKeys: modules.Select(m => m.ModuleKey).ToList());
    }
}
