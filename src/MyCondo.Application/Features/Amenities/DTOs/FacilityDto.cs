namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record FacilityDto(
    Guid FacilityId,
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
    bool BlocksEntryIfAccountOverdue,
    bool IsActive);
