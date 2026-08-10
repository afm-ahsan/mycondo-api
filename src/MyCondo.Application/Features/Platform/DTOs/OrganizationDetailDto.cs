namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record OrganizationDetailDto(
    Guid TenantId,
    string Name,
    string? Code,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    OrganizationAdministratorDto? Administrator,
    IReadOnlyList<string> EnabledModuleKeys);
