namespace MyCondo.Application.Features.Users.Queries.GetUserRoleAssignments;

public sealed record UserRoleAssignmentDto(
    Guid RoleId,
    string RoleName,
    string? Code,
    bool? RequiresBuildingScope,
    Guid? BuildingId
);
