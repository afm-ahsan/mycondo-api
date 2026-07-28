using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Tenancy.Events;

public sealed record TenantActivatedEvent(
    TenantId TenantId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
