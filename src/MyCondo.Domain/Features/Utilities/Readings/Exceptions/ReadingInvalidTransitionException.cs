using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Utilities.Readings.Exceptions;

public sealed class ReadingInvalidTransitionException(ReadingId readingId, ReadingStatus currentStatus, string attemptedAction)
    : DomainException($"Reading {readingId} in status {currentStatus} cannot {attemptedAction}.")
{
    public ReadingId ReadingId { get; } = readingId;
    public ReadingStatus CurrentStatus { get; } = currentStatus;
}
