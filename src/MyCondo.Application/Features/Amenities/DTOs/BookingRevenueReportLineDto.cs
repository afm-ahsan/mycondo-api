namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record BookingRevenueReportLineDto(
    Guid FacilityId,
    string FacilityName,
    decimal TotalBookingCharges,
    decimal TotalDepositsForfeited,
    int BillableBookingCount);
