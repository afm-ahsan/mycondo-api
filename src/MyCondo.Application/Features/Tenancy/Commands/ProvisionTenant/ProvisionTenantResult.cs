namespace MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;

public sealed record ProvisionTenantResult(
    Guid TenantId,
    string Name,
    string Slug,
    string Status
);
