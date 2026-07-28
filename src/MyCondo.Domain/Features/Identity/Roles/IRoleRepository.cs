namespace MyCondo.Domain.Features.Identity.Roles;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken);
    Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken);
    void Add(Role role);
}
