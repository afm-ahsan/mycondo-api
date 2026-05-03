using MediatR;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Common.Events;

/// <summary>
/// Wraps a Domain <see cref="IDomainEvent"/> so it can flow through MediatR's <c>INotification</c>
/// pipeline without making the Domain layer depend on MediatR.
/// Application layer event handlers are written as
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
