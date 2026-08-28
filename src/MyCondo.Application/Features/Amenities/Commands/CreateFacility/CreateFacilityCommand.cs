using Mediator;
using MyCondo.Application.Common.Abstractions;
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
) : IRequest<FacilityDto>, IRequiresFeature
{
    // FacilityType is required at creation (unlike the nullable filter on GetFacilitiesQuery), so the
    // feature this call actually uses is determinate from the request itself — no DB lookup needed.
    public string FeatureKey => FacilityType == "SwimmingPool" ? "facilities.swimming_pool" : "facilities.community_hall";
}
