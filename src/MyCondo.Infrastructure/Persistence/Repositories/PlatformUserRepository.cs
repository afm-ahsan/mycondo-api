using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
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

    public async Task<PagedResult<PlatformUser>> SearchAsync(
        string? searchText,
        PlatformUserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<PlatformUser> query = db.Set<PlatformUser>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.DisplayName, $"%{searchText}%")
                || EF.Functions.ILike(u.Email, $"%{searchText}%"));
        }

        if (status is PlatformUserStatus statusFilter)
        {
            query = query.Where(u => u.Status == statusFilter);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<PlatformUser> items = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformUser>(items, page, pageSize, total);
    }

    public void Add(PlatformUser platformUser) => db.Set<PlatformUser>().Add(platformUser);
}
