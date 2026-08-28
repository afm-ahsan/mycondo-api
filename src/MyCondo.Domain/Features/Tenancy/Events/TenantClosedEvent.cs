using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Tenancy.Events;

public sealed record TenantClosedEvent(
    TenantId TenantId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
