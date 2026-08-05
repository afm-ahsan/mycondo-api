namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;

public sealed record ServiceProviderAssignmentDto(
    Guid ServiceProviderAssignmentId,
    Guid ServiceProviderProfileId,
    Guid FlatId,
    bool ApprovedByResident,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime,
    bool IsActive);
