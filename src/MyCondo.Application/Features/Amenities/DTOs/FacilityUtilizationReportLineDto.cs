namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record FacilityUtilizationReportLineDto(
    Guid FacilityId,
    string FacilityName,
    int TotalBookings,
    int CompletedBookings,
    int CancelledBookings,
    int NoShowBookings);
