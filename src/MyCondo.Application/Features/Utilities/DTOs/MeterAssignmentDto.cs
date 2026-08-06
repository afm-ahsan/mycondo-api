namespace MyCondo.Application.Features.Utilities.DTOs;

public sealed record MeterAssignmentDto(
    Guid MeterAssignmentId,
    Guid MeterId,
    Guid FlatId,
    DateTimeOffset AssignedFromUtc,
    DateTimeOffset? AssignedToUtc);
