namespace MyCondo.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}
