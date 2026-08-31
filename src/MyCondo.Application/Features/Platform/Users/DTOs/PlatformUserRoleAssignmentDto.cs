namespace MyCondo.Application.Features.Platform.Users.DTOs;

public sealed record PlatformUserRoleAssignmentDto(
    Guid RoleId,
    string RoleName,
    DateTimeOffset GrantedAtUtc
);
