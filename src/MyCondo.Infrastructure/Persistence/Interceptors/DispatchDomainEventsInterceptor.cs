using Microsoft.EntityFrameworkCore.Diagnostics;
using MyCondo.Application.Common.Events;
using MyCondo.Domain.Common;

namespace MyCondo.Infrastructure.Persistence.Interceptors;

public sealed class DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return result;
        }

        List<IAggregateRoot> aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        List<IDomainEvent> events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (IAggregateRoot aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (IDomainEvent domainEvent in events)
        {
            await dispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        return result;
    }
}
