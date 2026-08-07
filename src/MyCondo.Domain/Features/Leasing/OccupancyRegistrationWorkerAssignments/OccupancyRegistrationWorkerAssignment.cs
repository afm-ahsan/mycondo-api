using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;

/// <summary>
/// Links an existing <see cref="DomesticWorkerProfile"/> (a domestic worker, cook, or — per
/// <see cref="Security.DomesticWorkers.DomesticWorkerType"/> — a <c>Driver</c>, which is not a
/// separate concept in this codebase) to an <see cref="OccupancyRegistration"/>. Deliberately a thin
/// relationship record owned by the Leasing module — the worker's identity, contact, and verification
/// status stay entirely on <see cref="DomesticWorkerProfile"/>, never duplicated here. This is
/// independent of <c>DomesticWorkerAssignment</c> (Security module, flat-scoped, gate-access
/// validity window) — that entity answers "can this worker enter this flat right now," this one
/// answers "which workers does this tenant registration currently claim."
/// </summary>
public sealed class OccupancyRegistrationWorkerAssignment
    : Entity<OccupancyRegistrationWorkerAssignmentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public OccupancyRegistrationId OccupancyRegistrationId { get; private set; }
    public DomesticWorkerProfileId DomesticWorkerProfileId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private OccupancyRegistrationWorkerAssignment() { }

    private OccupancyRegistrationWorkerAssignment(
        OccupancyRegistrationWorkerAssignmentId id, Guid tenantId, OccupancyRegistrationId occupancyRegistrationId,
        DomesticWorkerProfileId domesticWorkerProfileId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OccupancyRegistrationId = occupancyRegistrationId;
        DomesticWorkerProfileId = domesticWorkerProfileId;
        AssignedAtUtc = nowUtc;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public static OccupancyRegistrationWorkerAssignment Assign(
        Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, DomesticWorkerProfileId domesticWorkerProfileId,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new OccupancyRegistrationWorkerAssignment(
            OccupancyRegistrationWorkerAssignmentId.New(), tenantId, occupancyRegistrationId, domesticWorkerProfileId,
            nowUtc);
    }

    public void End(DateTimeOffset nowUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        EndedAtUtc = nowUtc;
    }
}
