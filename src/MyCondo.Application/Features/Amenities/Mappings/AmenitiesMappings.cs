using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Mappings;

internal static class AmenitiesMappings
{
    public static FacilityDto ToDto(this Facility facility) => new(
        facility.Id.Value, facility.BuildingId.Value, facility.Name, facility.FacilityType.ToString(),
        facility.Capacity, facility.OperatingHoursStart, facility.OperatingHoursEnd, facility.RequiresApproval,
        facility.BookingChargeAmount, facility.DepositAmount, facility.CancellationDeadlineHours,
        facility.CancellationDeductionPercentage, facility.GuestFeeAmount, facility.MinimumAgeUnaccompanied,
        facility.RequiresSafetyAcknowledgement, facility.BlocksEntryIfAccountOverdue, facility.IsActive);

    public static BlackoutDateDto ToDto(this BlackoutDate blackoutDate) => new(
        blackoutDate.Id.Value, blackoutDate.FacilityId.Value, blackoutDate.DateFrom, blackoutDate.DateTo,
        blackoutDate.Reason, blackoutDate.IsActive);

    public static BookingDto ToDto(this Booking booking) => new(
        booking.Id.Value, booking.FacilityId.Value, booking.BuildingId.Value, booking.FlatId.Value, booking.EventType,
        booking.StartAtUtc, booking.EndAtUtc, booking.SetupBufferMinutes, booking.CleanupBufferMinutes,
        booking.ExpectedGuestCount, booking.BookingChargeAmount, booking.DepositAmount,
        booking.CancellationDeadlineHours, booking.CancellationDeductionPercentage, booking.ApprovalRequired,
        booking.PaymentRequired, booking.Status.ToString(), booking.InvoiceId?.Value,
        booking.DepositCollectionPostingId?.Value, booking.DepositSettlementPostingId?.Value,
        booking.DepositRefundedAmount, booking.DepositDeductedAmount, booking.TermsAcceptedAtUtc, booking.ApprovedBy,
        booking.ApprovedAtUtc, booking.RejectedReason, booking.CancelledReason, booking.CancelledBy,
        booking.CancelledAtUtc, booking.CheckedInBy, booking.CheckedInAtUtc, booking.CompletedAtUtc,
        booking.InspectedBy, booking.InspectedAtUtc, booking.InspectionNotes, booking.DamageDeductionReason);

    public static PoolSessionDto ToDto(
        this PoolSession poolSession, string flatDisplayName, string checkedInByDisplayName, string? checkedOutByDisplayName) => new(
        poolSession.Id.Value, poolSession.FacilityId.Value, poolSession.FlatId.Value, flatDisplayName,
        poolSession.PersonType.ToString(), poolSession.AgeCategory.ToString(), poolSession.AccompaniedBySessionId?.Value,
        poolSession.EntryAtUtc, poolSession.ExitAtUtc, poolSession.GuestFeeAmount, poolSession.SafetyAcknowledgedAtUtc,
        poolSession.CheckedInBy, checkedInByDisplayName, poolSession.CheckedOutBy, checkedOutByDisplayName,
        poolSession.OverrideReason, poolSession.Status.ToString());

    public static PoolIncidentDto ToDto(this PoolIncident poolIncident) => new(
        poolIncident.Id.Value, poolIncident.FacilityId.Value, poolIncident.PoolSessionId?.Value,
        poolIncident.OccurredAtUtc, poolIncident.ReportedBy, poolIncident.Description, poolIncident.Severity.ToString(),
        poolIncident.ActionTaken);
}
