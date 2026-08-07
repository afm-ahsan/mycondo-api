namespace MyCondo.Application.Features.Leasing.DTOs;

public sealed record OccupancyRegistrationStatusHistoryDto(
    Guid OccupancyRegistrationStatusHistoryId,
    Guid OccupancyRegistrationId,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedBy,
    DateTimeOffset ChangedAtUtc,
    string? Reason);
