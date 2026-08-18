namespace MyCondo.Application.Features.Security.ParcelCustodyEvents.DTOs;

public sealed record ParcelCustodyEventDto(
    Guid ParcelCustodyEventId,
    Guid ParcelId,
    string ToStatus,
    DateTimeOffset OccurredAtUtc,
    Guid? PerformedBy,
    string PerformedByDisplayName,
    string? Notes);
