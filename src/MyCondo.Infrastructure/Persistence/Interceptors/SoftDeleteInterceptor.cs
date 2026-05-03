using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;

namespace MyCondo.Infrastructure.Persistence.Interceptors;

public sealed class SoftDeleteInterceptor(
    IClock clock,
    ICurrentUserProvider currentUser
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        Guid? userId = currentUser.UserId;

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ISoftDeletable> entry
                 in eventData.Context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAtUtc = nowUtc;
                entry.Entity.DeletedBy = userId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
