namespace MyCondo.Application.Features.Platform.Users.DTOs;

public sealed record PlatformUserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> RoleNames
);
