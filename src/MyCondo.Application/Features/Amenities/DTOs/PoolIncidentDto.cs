namespace MyCondo.Application.Features.Amenities.DTOs;

public sealed record PoolIncidentDto(
    Guid PoolIncidentId,
    Guid FacilityId,
    Guid? PoolSessionId,
    DateTimeOffset OccurredAtUtc,
    Guid? ReportedBy,
    string Description,
    string Severity,
    string? ActionTaken);
