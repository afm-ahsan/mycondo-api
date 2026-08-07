namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record PoolDailyUsageReportDto(
    DateOnly Date,
    int TotalEntries,
    int ResidentEntries,
    int GuestEntries,
    int AdultEntries,
    int ChildEntries,
    int PeakConcurrentOccupancy);
