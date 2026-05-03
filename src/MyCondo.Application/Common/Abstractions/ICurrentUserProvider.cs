namespace MyCondo.Application.Common.Abstractions;

public interface ICurrentUserProvider
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    IReadOnlyList<Guid> BuildingIds { get; }
}
