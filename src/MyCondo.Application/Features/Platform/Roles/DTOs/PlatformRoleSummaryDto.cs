namespace MyCondo.Application.Features.Platform.Roles.DTOs;

public sealed record PlatformRoleSummaryDto(
    Guid Id,
    string Name,
    string Description
);
