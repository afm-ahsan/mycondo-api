using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformUserRepository(MyCondoDbContext db) : IPlatformUserRepository
{
    public Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken) =>
        db.Set<PlatformUser>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Set<PlatformUser>().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        db.Set<PlatformUser>().AnyAsync(cancellationToken);

    public void Add(PlatformUser platformUser) => db.Set<PlatformUser>().Add(platformUser);
}
