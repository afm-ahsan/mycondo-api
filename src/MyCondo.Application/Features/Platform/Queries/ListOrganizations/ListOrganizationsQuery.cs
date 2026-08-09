using Mediator;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

namespace MyCondo.Application.Features.Platform.Queries.ListOrganizations;

/// <summary>
/// Platform-scope read of every organization (Tenant) — reuses the existing tenant-side
/// <see cref="TenantSummaryDto"/> shape rather than inventing a parallel "OrganizationSummaryDto".
/// Not RLS-filtered: <c>tenancy.tenants</c> has no tenant_id/RLS policy (it IS the tenant registry).
/// </summary>
public sealed record ListOrganizationsQuery : IRequest<IReadOnlyList<TenantSummaryDto>>;
