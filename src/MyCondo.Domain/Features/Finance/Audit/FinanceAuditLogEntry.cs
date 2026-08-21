using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Finance.Audit;

/// <summary>
/// Append-only record of a security-sensitive Finance operation — period close/soft-close/reopen,
/// reversal/void/waiver, account-mapping change, Fixed Deposit void/renewal, bank-reconciliation
/// completion, and approval actions (Template 6, "Auditability"). Tenant-scoped, RLS-protected
/// counterpart to <see cref="MyCondo.Domain.Features.Platform.PlatformAudit.PlatformAuditLogEntry"/> —
/// same append-only shape, deliberately not reused directly since that type lives in the
/// non-tenant-scoped <c>platform</c> schema (see its own doc comment).
///
/// Deliberately does not cover every routine posting (invoice issuance, ordinary payment receipt,
/// FD placement) — those are already durably, immutably recorded as <c>LedgerPosting</c>/
/// <c>LedgerEntry</c> rows, which is itself the audit trail for "create/post". This log exists for the
/// governance-layer actions that have no other durable record: a state transition, a config change, or
/// a correction to something already posted.
///
/// Never carries secrets; <see cref="Metadata"/> is a small free-form JSON blob for non-secret context
/// only.
/// </summary>
public sealed class FinanceAuditLogEntry : Entity<FinanceAuditLogEntryId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? Metadata { get; private set; }
    public string? CorrelationId { get; private set; }

    private FinanceAuditLogEntry()
    {
        Action = null!;
    }

    private FinanceAuditLogEntry(
        FinanceAuditLogEntryId id,
        Guid tenantId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string action,
        string? targetType,
        string? targetId,
        string? metadata,
        string? correlationId) : base(id)
    {
        TenantId = tenantId;
        OccurredAtUtc = occurredAtUtc;
        ActorUserId = actorUserId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        Metadata = metadata;
        CorrelationId = correlationId;
    }

    public static FinanceAuditLogEntry Record(
        Guid tenantId,
        DateTimeOffset nowUtc,
        Guid? actorUserId,
        string action,
        string? targetType = null,
        string? targetId = null,
        string? metadata = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new FinanceAuditLogEntry(
            FinanceAuditLogEntryId.New(), tenantId, nowUtc, actorUserId, action.Trim(),
            targetType, targetId, metadata, correlationId);
    }
}
