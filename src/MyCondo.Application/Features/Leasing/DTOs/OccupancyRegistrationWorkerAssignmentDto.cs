namespace MyCondo.Application.Features.Leasing.DTOs;

public sealed record OccupancyRegistrationWorkerAssignmentDto(
    Guid OccupancyRegistrationWorkerAssignmentId,
    Guid OccupancyRegistrationId,
    Guid DomesticWorkerProfileId,
    string WorkerFullName,
    string WorkerPhone,
    string WorkerType,
    string VerificationStatus,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? EndedAtUtc,
    bool IsActive);
