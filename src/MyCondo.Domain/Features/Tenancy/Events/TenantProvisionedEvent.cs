using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Tenancy.Events;

public sealed record TenantProvisionedEvent(
    TenantId TenantId,
    string Name,
    string Slug,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
