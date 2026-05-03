using MyCondo.Domain.Common;

namespace MyCondo.Application.Common.Events;

/// <summary>
/// Handler for a Domain event raised by an aggregate. Multiple handlers per event are allowed.
/// Implementations live in <c>Application/{Aggregate}/EventHandlers/</c> and are auto-registered
/// in DI by <c>AddApplication()</c>.
///
/// Domain events bypass Mediator's IRequest/INotification pipeline by design: the pipeline's
/// validation/logging/performance behaviors target commands+queries; domain-event side effects
/// shouldn't get the same treatment, and Mediator's source generator dislikes open-generic
/// notifications without concrete handlers.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    ValueTask HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
