using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CreateFacility;

public sealed record CreateFacilityCommand(
    Guid BuildingId,
    string Name,
    string FacilityType,
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
