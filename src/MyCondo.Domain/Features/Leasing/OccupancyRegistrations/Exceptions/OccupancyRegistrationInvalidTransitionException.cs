using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrations.Exceptions;

public sealed class OccupancyRegistrationInvalidTransitionException(
    OccupancyRegistrationId id, OccupancyRegistrationStatus currentStatus, string attemptedAction)
    : DomainException($"Occupancy registration {id} cannot {attemptedAction} while Status is {currentStatus}.")
{
    public OccupancyRegistrationId OccupancyRegistrationId { get; } = id;
}
