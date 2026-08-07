namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record BlackoutDateDto(
    Guid BlackoutDateId,
    Guid FacilityId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Reason,
    bool IsActive);
