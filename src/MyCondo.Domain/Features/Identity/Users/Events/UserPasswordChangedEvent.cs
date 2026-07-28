using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Users.Events;

public sealed record UserPasswordChangedEvent(
    UserId UserId,
    Guid TenantId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
