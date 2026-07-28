using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Users.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    Guid TenantId,
    string Email,
    string FullName,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
