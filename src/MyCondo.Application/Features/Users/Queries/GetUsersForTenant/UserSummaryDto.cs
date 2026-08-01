namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    bool IsActive
);
