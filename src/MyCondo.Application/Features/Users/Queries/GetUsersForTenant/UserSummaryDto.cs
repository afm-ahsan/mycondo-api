namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> RoleNames
);
