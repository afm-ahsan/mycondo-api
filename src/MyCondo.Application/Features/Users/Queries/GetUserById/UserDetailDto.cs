namespace MyCondo.Application.Features.Users.Queries.GetUserById;

public sealed record UserDetailDto(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    bool EmailConfirmed,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);
