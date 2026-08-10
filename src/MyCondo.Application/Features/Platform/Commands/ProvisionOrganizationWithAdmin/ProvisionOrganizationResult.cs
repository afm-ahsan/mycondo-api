namespace MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

public sealed record ProvisionOrganizationResult(
    Guid TenantId,
    string Name,
    string Code,
    string Slug,
    string Status,
    Guid AdministratorUserId);
