using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformRoleRepository(MyCondoDbContext db) : IPlatformRoleRepository
{
    public Task<PlatformRole?> GetByIdAsync(PlatformRoleId id, CancellationToken cancellationToken) =>
        db.Set<PlatformRole>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<PlatformRole?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        db.Set<PlatformRole>().FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public void Add(PlatformRole platformRole) => db.Set<PlatformRole>().Add(platformRole);
}
