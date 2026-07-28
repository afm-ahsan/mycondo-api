using MyCondo.Application.Common.Abstractions;

namespace MyCondo.DbMigrator;

/// <summary>
/// This tool runs outside any HTTP request, so there is no authenticated caller — audit columns
/// (CreatedBy/UpdatedBy) end up null for rows this tool writes; the operator's identity is captured
/// separately in the tool's own structured log line instead (see Program.cs).
/// </summary>
public sealed class NullCurrentUserProvider : ICurrentUserProvider
{
    public Guid? UserId => null;
    public Guid? TenantId => null;
    public bool IsAuthenticated => false;
    public bool IsInRole(string role) => false;
    public bool HasPermission(string permission) => false;
    public bool HasPermissionForBuilding(string permission, Guid? buildingId) => false;
    public IReadOnlyList<Guid> BuildingIds => [];
}
