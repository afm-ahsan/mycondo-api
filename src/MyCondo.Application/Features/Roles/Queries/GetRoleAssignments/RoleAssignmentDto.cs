namespace MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;

public sealed record RoleAssignmentDto(
    Guid UserId,
    string Email,
    string FullName,
    Guid? BuildingId
);
