using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Gates;

/// <summary>
/// A named entry/exit point of a building (e.g. "Main Gate", "Basement Ramp"). Referenced by every
/// access-session register (car/guest/staff/teacher in-out, Slice B onward) as the entry/exit gate —
/// kept as simple reference data here: no soft-delete/concurrency, since gates are rarely edited once
/// created and nothing depends on optimistic-concurrency guarantees for this entity yet.
/// </summary>
public sealed class Gate : AggregateRoot<GateId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string Name { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Gate()
    {
        Name = null!;
    }

    private Gate(GateId id, Guid tenantId, BuildingId buildingId, string name, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        Name = name;
        CreatedAtUtc = nowUtc;
    }

    public static Gate Create(Guid tenantId, BuildingId buildingId, string name, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Gate(GateId.New(), tenantId, buildingId, name.Trim(), nowUtc);
    }
}
