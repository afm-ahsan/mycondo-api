using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Audit;

/// <summary>
/// Append-only record of a security-sensitive tenant identity operation (mycondo-docs ADR-035) — user
/// create/edit/activate/deactivate, role assigned/revoked, and a privileged-target action denied by
/// <c>ITenantAdminProtectionService</c> (self-protection, target-privilege, or last-active-admin
/// rejection). Tenant-scoped, RLS-protected — same append-only shape as
/// <see cref="MyCondo.Domain.Features.Finance.Audit.FinanceAuditLogEntry"/>, deliberately not reused
/// directly (same reasoning as that type's own doc comment: a distinct governance-layer log per bounded
/// context, not a single shared audit table).
///
/// Never carries secrets — passwords, password hashes, or tokens; <see cref="Metadata"/> is a small
/// free-form JSON blob for non-secret context only.
/// </summary>
public sealed class IdentityAuditLogEntry : Entity<IdentityAuditLogEntryId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? Metadata { get; private set; }
    public string? CorrelationId { get; private set; }

    private IdentityAuditLogEntry()
    {
        Action = null!;
    }

    private IdentityAuditLogEntry(
        IdentityAuditLogEntryId id,
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

    public static IdentityAuditLogEntry Record(
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

        return new IdentityAuditLogEntry(
            IdentityAuditLogEntryId.New(), tenantId, nowUtc, actorUserId, action.Trim(),
            targetType, targetId, metadata, correlationId);
    }
}
