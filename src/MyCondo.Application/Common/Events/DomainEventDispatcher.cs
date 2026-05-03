using System.Collections;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Common.Events;

public sealed class DomainEventDispatcher(
    IServiceProvider services,
    ILogger<DomainEventDispatcher> logger
) : IDomainEventDispatcher
{
    public async ValueTask DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Type eventType = domainEvent.GetType();
        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        Type enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);

        IEnumerable handlers = (IEnumerable)services.GetRequiredService(enumerableType);

        MethodInfo? handle = handlerType.GetMethod(nameof(IDomainEventHandler<DummyEvent>.HandleAsync));
        if (handle is null)
        {
            return;
        }

        foreach (object handler in handlers)
        {
            try
            {
                ValueTask task = (ValueTask)handle.Invoke(handler, [domainEvent, cancellationToken])!;
                await task;
            }
            catch (Exception ex)
            {
                // Domain-event handlers must be idempotent; swallow so one bad handler doesn't break
                // the pipeline. Outbox + retry semantics live elsewhere when needed.
                logger.LogError(ex,
                    "Domain event handler {Handler} failed for {EventType}",
                    handler.GetType().Name, eventType.Name);
            }
        }
    }

    /// <summary>Phantom type used only to obtain the <c>HandleAsync</c> <see cref="MethodInfo"/>.</summary>
    private sealed record DummyEvent : IDomainEvent
    {
        public Guid EventId => Guid.Empty;
        public DateTimeOffset OccurredAtUtc => DateTimeOffset.MinValue;
    }
}
