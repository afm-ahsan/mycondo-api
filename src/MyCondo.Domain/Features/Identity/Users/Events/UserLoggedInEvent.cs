using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Users.Events;

public sealed record UserLoggedInEvent(
    UserId UserId,
    Guid TenantId,
    string IpAddress,
    DateTimeOffset OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
}
