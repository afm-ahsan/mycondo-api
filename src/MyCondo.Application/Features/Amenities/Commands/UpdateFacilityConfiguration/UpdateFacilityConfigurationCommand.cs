using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.UpdateFacilityConfiguration;

public sealed record UpdateFacilityConfigurationCommand(
    Guid FacilityId,
    string Name,
    int Capacity,
    TimeOnly? OperatingHoursStart,
    TimeOnly? OperatingHoursEnd,
    bool RequiresApproval,
    decimal? BookingChargeAmount,
    decimal? DepositAmount,
    int CancellationDeadlineHours,
    decimal CancellationDeductionPercentage,
    decimal? GuestFeeAmount,
    int? MinimumAgeUnaccompanied,
    bool RequiresSafetyAcknowledgement,
    bool BlocksEntryIfAccountOverdue
) : IRequest<FacilityDto>;
