namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;

public sealed record DomesticWorkerAssignmentDto(
    Guid DomesticWorkerAssignmentId,
    Guid DomesticWorkerProfileId,
    Guid FlatId,
    bool ApprovedByResident,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime,
    bool IsActive);
