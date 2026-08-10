using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Platform.Queries.ListOrganizations;

/// <summary>
/// Platform-scope, paginated read of every organization (Tenant), with an optional case-insensitive
/// name/code/slug search term and/or status filter. Not RLS-filtered: <c>tenancy.tenants</c> has no
/// tenant_id/RLS policy (it IS the tenant registry).
/// </summary>
public sealed record ListOrganizationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null
) : IRequest<PagedResult<OrganizationListItemDto>>;
