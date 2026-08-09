namespace MyCondo.Domain.Features.Identity.Permissions;

public interface IPermissionRepository
{
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Used by PlatformBootstrapSeeder to grant the Platform SuperAdmin role exactly the
    /// "Platform" module's permissions — never the full tenant catalog.</summary>
    Task<List<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken);

    Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(PermissionId id, CancellationToken cancellationToken);

    /// <summary>Used by PermissionSeeder to add catalogue entries missing from identity.permissions.
    /// Never used to update or remove an existing row — reconciliation is additive-only.</summary>
    void Add(Permission permission);
}
