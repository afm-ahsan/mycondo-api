using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Platform.PlatformAudit;

/// <summary>
/// Append-only record of a security-sensitive platform-scope operation (login, organization
/// provisioning/suspension, platform role/permission changes). Lives in the <c>platform</c> schema —
/// not tenant data, so no RLS, same reasoning as every other platform table. Never carries passwords,
/// password hashes, refresh-token secrets, or access tokens; <see cref="Metadata"/> is a small
/// free-form JSON blob for non-secret context only (e.g. the attempted email on a failed login, for
/// abuse monitoring — never leaked back to the caller, see PlatformLoginCommandHandler).
///
/// <see cref="TenantId"/> is the one field on this entity that looks, at a glance, like it could be a
/// tenant-membership column — it is not. Read its own doc comment before assuming otherwise; this
/// class has no property that associates a <see cref="MyCondo.Domain.Features.Platform.PlatformUsers.PlatformUser"/>
/// with any tenant.
/// </summary>
public sealed class PlatformAuditLogEntry : Entity<PlatformAuditLogEntryId>
{
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorPlatformUserId { get; private set; }
    public string Action { get; private set; }
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }

    /// <summary>
    /// The tenant (Organization) a platform-initiated action affected — e.g. which tenant was
    /// suspended, activated, or created — NOT the platform actor's own tenant membership. A
    /// <see cref="MyCondo.Domain.Features.Platform.PlatformUsers.PlatformUser"/> has no tenant
    /// membership at all (see its own doc comment and mycondo-docs ADR-019); this column exists purely
    /// so an auditor can ask "which platform actions touched tenant X," the same way
    /// <see cref="TargetType"/>/<see cref="TargetId"/> answer "which record did this action touch."
    /// Null for actions with no single-tenant target (login, permission-catalog changes, etc.).
    /// </summary>
    public Guid? TenantId { get; private set; }

    public string? Metadata { get; private set; }
    public string? CorrelationId { get; private set; }

    private PlatformAuditLogEntry()
    {
        Action = null!;
    }

    private PlatformAuditLogEntry(
        PlatformAuditLogEntryId id,
        DateTimeOffset occurredAtUtc,
        Guid? actorPlatformUserId,
        string action,
        string? targetType,
        string? targetId,
        Guid? tenantId,
        string? metadata,
        string? correlationId) : base(id)
    {
        OccurredAtUtc = occurredAtUtc;
        ActorPlatformUserId = actorPlatformUserId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        TenantId = tenantId;
        Metadata = metadata;
        CorrelationId = correlationId;
    }

    public static PlatformAuditLogEntry Record(
        DateTimeOffset nowUtc,
        Guid? actorPlatformUserId,
        string action,
        string? targetType = null,
        string? targetId = null,
        Guid? tenantId = null,
        string? metadata = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return new PlatformAuditLogEntry(
            PlatformAuditLogEntryId.New(), nowUtc, actorPlatformUserId, action.Trim(),
            targetType, targetId, tenantId, metadata, correlationId);
    }
}
