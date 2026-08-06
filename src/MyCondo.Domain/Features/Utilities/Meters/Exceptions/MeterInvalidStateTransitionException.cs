using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Utilities.Meters.Exceptions;

public sealed class MeterInvalidStateTransitionException(MeterId meterId, MeterStatus currentStatus, string attemptedAction)
    : DomainException($"Meter {meterId} in status {currentStatus} cannot {attemptedAction}.")
{
    public MeterId MeterId { get; } = meterId;
    public MeterStatus CurrentStatus { get; } = currentStatus;
}
