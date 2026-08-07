using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Domain.Features.Amenities.PoolIncidents;

/// <summary>Create-only incident record at a Swimming Pool facility — no status lifecycle per spec
/// §5.12, just a record of what happened and what action was taken.</summary>
public sealed class PoolIncident : Entity<PoolIncidentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FacilityId FacilityId { get; private set; }
    public PoolSessionId? PoolSessionId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ReportedBy { get; private set; }
    public string Description { get; private set; }
    public PoolIncidentSeverity Severity { get; private set; }
    public string? ActionTaken { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private PoolIncident()
    {
        Description = null!;
    }

    private PoolIncident(
        PoolIncidentId id, Guid tenantId, FacilityId facilityId, PoolSessionId? poolSessionId, DateTimeOffset occurredAtUtc,
        Guid? reportedBy, string description, PoolIncidentSeverity severity, string? actionTaken, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        FacilityId = facilityId;
        PoolSessionId = poolSessionId;
        OccurredAtUtc = occurredAtUtc;
        ReportedBy = reportedBy;
        Description = description;
        Severity = severity;
        ActionTaken = actionTaken;
        CreatedAtUtc = nowUtc;
    }

    public static PoolIncident Report(
        Guid tenantId, FacilityId facilityId, PoolSessionId? poolSessionId, DateTimeOffset occurredAtUtc,
        Guid? reportedBy, string description, PoolIncidentSeverity severity, string? actionTaken, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new PoolIncident(
            PoolIncidentId.New(), tenantId, facilityId, poolSessionId, occurredAtUtc, reportedBy, description.Trim(),
            severity, actionTaken?.Trim(), nowUtc);
    }
}
