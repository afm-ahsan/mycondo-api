using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;

/// <summary>
/// Links an existing <see cref="Vehicle"/> to an <see cref="OccupancyRegistration"/>. Thin
/// relationship record owned by the Leasing module — vehicle data (registration number, type,
/// ownership category, its own flat linkage) stays entirely owned by the Security/Vehicles module,
/// never duplicated here.
/// </summary>
public sealed class OccupancyRegistrationVehicleAssignment
    : Entity<OccupancyRegistrationVehicleAssignmentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public OccupancyRegistrationId OccupancyRegistrationId { get; private set; }
    public VehicleId VehicleId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private OccupancyRegistrationVehicleAssignment() { }

    private OccupancyRegistrationVehicleAssignment(
        OccupancyRegistrationVehicleAssignmentId id, Guid tenantId, OccupancyRegistrationId occupancyRegistrationId,
        VehicleId vehicleId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OccupancyRegistrationId = occupancyRegistrationId;
        VehicleId = vehicleId;
        AssignedAtUtc = nowUtc;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public static OccupancyRegistrationVehicleAssignment Assign(
        Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, VehicleId vehicleId, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new OccupancyRegistrationVehicleAssignment(
            OccupancyRegistrationVehicleAssignmentId.New(), tenantId, occupancyRegistrationId, vehicleId, nowUtc);
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
