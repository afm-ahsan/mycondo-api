namespace MyCondo.Application.Features.Security.DTOs;

public sealed record CurrentlyInsideCategoryCountDto(string Category, int Count);

/// <summary>Tenant-wide only — <c>AccessSession</c> and <c>Parcel</c> have no BuildingId of their own
/// (one front gate/desk serves the whole property), so no buildingId filter is offered here. Excludes
/// Payroll's Staff Attendance entirely — that stays Payroll's domain, not Security's, per UX-5's
/// approved module-boundary decision.</summary>
public sealed record SecuritySummaryDto(
    IReadOnlyList<CurrentlyInsideCategoryCountDto> CurrentlyInside,
    int ParcelsAwaitingCollectionCount
);
