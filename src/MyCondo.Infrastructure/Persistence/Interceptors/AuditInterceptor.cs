using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;

namespace MyCondo.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor(
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

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IAuditable> entry
                 in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = nowUtc;
                entry.Entity.UpdatedBy = userId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
