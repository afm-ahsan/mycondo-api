using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.ListOrganizations;

public sealed class ListOrganizationsQueryHandler(
    ITenantRepository tenants,
    ITenantModuleRepository tenantModules
) : IRequestHandler<ListOrganizationsQuery, PagedResult<OrganizationListItemDto>>
{
    public async ValueTask<PagedResult<OrganizationListItemDto>> Handle(
        ListOrganizationsQuery query, CancellationToken cancellationToken)
    {
        TenantStatus? status = !string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse(query.Status, out TenantStatus parsed)
            ? parsed
            : null;

        PagedResult<Tenant> page = await tenants.SearchAsync(
            query.Page, query.PageSize, query.Search, status, cancellationToken);

        List<Guid> tenantIds = page.Items.Select(t => t.Id.Value).ToList();
        Dictionary<Guid, int> moduleCounts = await tenantModules.GetEnabledCountsAsync(tenantIds, cancellationToken);

        List<OrganizationListItemDto> items = page.Items
            .Select(t => new OrganizationListItemDto(
                TenantId: t.Id.Value,
                Name: t.Name,
                Code: t.Code,
                Slug: t.Slug,
                Status: t.Status.ToString(),
                PrimaryAdministratorFullName: t.PrimaryAdministratorFullName,
                PrimaryAdministratorEmail: t.PrimaryAdministratorEmail,
                CreatedAtUtc: t.CreatedAtUtc,
                EnabledModuleCount: moduleCounts.GetValueOrDefault(t.Id.Value)))
            .ToList();

        return new PagedResult<OrganizationListItemDto>(items, page.Page, page.PageSize, page.Total);
    }
}
