namespace MyCondo.Application.Features.Identity.Audit.DTOs;

public sealed record IdentityAuditLogEntryDto(
    Guid IdentityAuditLogEntryId,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    string ActorDisplayName,
    string Action,
    string? TargetType,
    string? TargetId,
    string? Metadata,
    string? CorrelationId);
