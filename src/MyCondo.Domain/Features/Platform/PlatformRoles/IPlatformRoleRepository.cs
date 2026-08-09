namespace MyCondo.Domain.Features.Platform.PlatformRoles;

public interface IPlatformRoleRepository
{
    Task<PlatformRole?> GetByIdAsync(PlatformRoleId id, CancellationToken cancellationToken);

    Task<PlatformRole?> GetByNameAsync(string name, CancellationToken cancellationToken);

    void Add(PlatformRole platformRole);
}
