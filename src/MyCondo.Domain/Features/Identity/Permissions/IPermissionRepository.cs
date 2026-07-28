namespace MyCondo.Domain.Features.Identity.Permissions;

public interface IPermissionRepository
{
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken);
    Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(PermissionId id, CancellationToken cancellationToken);
}
