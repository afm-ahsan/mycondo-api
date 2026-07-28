namespace MyCondo.Application.Common.Abstractions;

public interface ICurrentUserProvider
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);

    /// <summary>
    /// True if the caller holds <paramref name="permission"/> tenant-wide, or — when
    /// <paramref name="buildingId"/> is supplied — for that specific building. A null
    /// <paramref name="buildingId"/> means the caller is asking about a non-building-scoped
    /// operation, so this is equivalent to <see cref="HasPermission"/>. See ADR-014.
    /// </summary>
    bool HasPermissionForBuilding(string permission, Guid? buildingId);

    IReadOnlyList<Guid> BuildingIds { get; }
}
