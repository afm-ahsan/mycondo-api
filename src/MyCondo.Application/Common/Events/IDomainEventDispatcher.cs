using MyCondo.Domain.Common;

namespace MyCondo.Application.Common.Events;

/// <summary>
/// Publishes Domain events to their handlers. Implemented in Application using Mediator
/// so Infrastructure (the EF Core SaveChangesInterceptor that calls this) doesn't need
/// a direct Mediator reference.
/// </summary>
public interface IDomainEventDispatcher
{
    ValueTask DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
