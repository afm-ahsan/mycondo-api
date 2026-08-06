using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters.Exceptions;

namespace MyCondo.Domain.Features.Utilities.Meters;

/// <summary>
/// A single shared aggregate for both electricity and gas meters, discriminated by
/// <see cref="UtilityType"/> — mirrors the <c>AccessSession</c> pattern from Slice B rather than two
/// parallel entity sets. <see cref="ReplacesMeterId"/> only ever points backwards to the meter this
/// one replaced; walk that chain to reconstruct full replacement history — no forward pointer is
/// needed since a meter is replaced at most once (a second replacement replaces the newest meter,
/// which then points back to this one).
/// </summary>
public sealed class Meter : AggregateRoot<MeterId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public UtilityType UtilityType { get; private set; }
    public string MeterNumber { get; private set; }
    public MeterStatus Status { get; private set; }
    public MeterId? ReplacesMeterId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Meter()
    {
        MeterNumber = null!;
    }

    private Meter(
        MeterId id,
        Guid tenantId,
        BuildingId buildingId,
        UtilityType utilityType,
        string meterNumber,
        MeterId? replacesMeterId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        UtilityType = utilityType;
        MeterNumber = meterNumber;
        Status = MeterStatus.Active;
        ReplacesMeterId = replacesMeterId;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Meter Install(
        Guid tenantId, BuildingId buildingId, UtilityType utilityType, string meterNumber, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meterNumber);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Meter(MeterId.New(), tenantId, buildingId, utilityType, meterNumber.Trim(), null, nowUtc);
    }

    public void MarkFaulty(string reason)
    {
        if (Status != MeterStatus.Active)
        {
            throw new MeterInvalidStateTransitionException(Id, Status, "be marked faulty (only Active meters can)");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = MeterStatus.Faulty;
        Version++;
    }

    public void Reactivate()
    {
        if (Status is not (MeterStatus.Faulty or MeterStatus.Inactive))
        {
            throw new MeterInvalidStateTransitionException(Id, Status, "be reactivated");
        }

        Status = MeterStatus.Active;
        Version++;
    }

    public void Deactivate()
    {
        if (Status is not (MeterStatus.Active or MeterStatus.Faulty))
        {
            throw new MeterInvalidStateTransitionException(Id, Status, "be deactivated");
        }

        Status = MeterStatus.Inactive;
        Version++;
    }

    /// <summary>Marks this meter <see cref="MeterStatus.Replaced"/> and returns a brand-new
    /// <see cref="Meter"/> (same tenant/building/utility type) with <see cref="ReplacesMeterId"/>
    /// pointing back to this one's id.</summary>
    public Meter ReplaceWith(string newMeterNumber, DateTimeOffset nowUtc)
    {
        if (Status == MeterStatus.Replaced)
        {
            throw new MeterInvalidStateTransitionException(Id, Status, "be replaced again (already replaced)");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newMeterNumber);

        Status = MeterStatus.Replaced;
        Version++;

        return new Meter(MeterId.New(), TenantId, BuildingId, UtilityType, newMeterNumber.Trim(), Id, nowUtc);
    }
}
