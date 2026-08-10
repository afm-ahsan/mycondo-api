namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record OrganizationListItemDto(
    Guid TenantId,
    string Name,
    string? Code,
    string Slug,
    string Status,
    string? PrimaryAdministratorFullName,
    string? PrimaryAdministratorEmail,
    DateTimeOffset CreatedAtUtc,
    int EnabledModuleCount);
