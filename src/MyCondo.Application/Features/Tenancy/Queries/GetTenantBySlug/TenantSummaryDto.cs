namespace MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

public sealed record TenantSummaryDto(
    Guid TenantId,
    string Name,
    string Slug,
    string Status
);
