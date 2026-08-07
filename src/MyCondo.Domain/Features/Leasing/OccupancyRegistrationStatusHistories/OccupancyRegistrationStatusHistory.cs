using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

/// <summary>
/// One status transition of an <see cref="OccupancyRegistration"/> — append-only, never edited or
/// deleted, matching the append-only ledger philosophy used elsewhere in this codebase (see
/// <c>CylinderStockMovement</c>). This is the first real audit/history table in the application; today
/// every other feature only has <c>IAuditable</c> Created/Updated timestamps, which cannot show a full
/// lifecycle. Recorded by the application layer inside the same transaction as each status-changing
/// command, one row per transition.
/// </summary>
public sealed class OccupancyRegistrationStatusHistory : Entity<OccupancyRegistrationStatusHistoryId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public OccupancyRegistrationId OccupancyRegistrationId { get; private set; }
    public OccupancyRegistrationStatus? FromStatus { get; private set; }
    public OccupancyRegistrationStatus ToStatus { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    private OccupancyRegistrationStatusHistory() { }

    private OccupancyRegistrationStatusHistory(
        OccupancyRegistrationStatusHistoryId id, Guid tenantId, OccupancyRegistrationId occupancyRegistrationId,
        OccupancyRegistrationStatus? fromStatus, OccupancyRegistrationStatus toStatus, Guid? changedBy,
        string? reason, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OccupancyRegistrationId = occupancyRegistrationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedBy = changedBy;
        Reason = reason;
        ChangedAtUtc = nowUtc;
    }

    public static OccupancyRegistrationStatusHistory Record(
        Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, OccupancyRegistrationStatus? fromStatus,
        OccupancyRegistrationStatus toStatus, Guid? changedBy, string? reason, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new OccupancyRegistrationStatusHistory(
            OccupancyRegistrationStatusHistoryId.New(), tenantId, occupancyRegistrationId, fromStatus, toStatus,
            changedBy, reason?.Trim(), nowUtc);
    }
}
