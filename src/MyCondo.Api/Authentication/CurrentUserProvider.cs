using System.Security.Claims;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

public sealed class CurrentUserProvider(IHttpContextAccessor http) : ICurrentUserProvider
{
    private const string TenantClaim = "tenant_id";
    private const string PermissionClaim = "perm";
    private const string BuildingClaim = "building_ids";
    private const string BuildingPermissionClaim = "bperm";

    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            string? sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out Guid id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            string? raw = Principal?.FindFirstValue(TenantClaim);
            return Guid.TryParse(raw, out Guid id) ? id : null;
        }
    }

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public bool HasPermission(string permission) =>
        Principal?.Claims.Any(c =>
            string.Equals(c.Type, PermissionClaim, StringComparison.Ordinal)
            && string.Equals(c.Value, permission, StringComparison.Ordinal)) ?? false;

    public bool HasPermissionForBuilding(string permission, Guid? buildingId)
    {
        if (HasPermission(permission))
        {
            // A tenant-wide grant applies everywhere, including this building.
            return true;
        }

        if (buildingId is null)
        {
            return false;
        }

        string expected = $"{buildingId.Value}|{permission}";
        return Principal?.Claims.Any(c =>
            string.Equals(c.Type, BuildingPermissionClaim, StringComparison.Ordinal)
            && string.Equals(c.Value, expected, StringComparison.Ordinal)) ?? false;
    }

    public IReadOnlyList<Guid> BuildingIds =>
        Principal?.FindAll(BuildingClaim)
            .Select(c => Guid.TryParse(c.Value, out Guid g) ? g : (Guid?)null)
            .Where(g => g is not null)
            .Select(g => g!.Value)
            .ToList() ?? [];
}
