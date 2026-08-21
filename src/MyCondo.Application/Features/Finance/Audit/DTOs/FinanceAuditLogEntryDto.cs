namespace MyCondo.Application.Features.Finance.Audit.DTOs;

public sealed record FinanceAuditLogEntryDto(
    Guid FinanceAuditLogEntryId,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    string Action,
    string? TargetType,
    string? TargetId,
    string? Metadata,
    string? CorrelationId);
