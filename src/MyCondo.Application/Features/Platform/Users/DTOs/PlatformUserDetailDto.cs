namespace MyCondo.Application.Features.Platform.Users.DTOs;

public sealed record PlatformUserDetailDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);
