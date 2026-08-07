using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions.Exceptions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Amenities.PoolSessions;

/// <summary>
/// One entry/exit record at a Swimming Pool <see cref="Facility"/>. Deliberately its own aggregate,
/// not a reuse of Security's <c>AccessSession</c> — pool entry needs capacity, eligibility, guest fees,
/// and incident tracking that don't fit that type's shape (see plan §6 for the resolved fork).
/// Capacity enforcement, blackout-window checks, safety-acknowledgement, minimum-age/accompaniment,
/// and outstanding-balance eligibility are all cross-aggregate/config-dependent checks made by the
/// Application-layer handler before <see cref="CheckIn"/> is called — this aggregate stores the
/// outcome (including <see cref="OverrideReason"/> when a `pool.override`-holder bypassed a rule), it
/// does not re-derive eligibility itself (same division of responsibility as <c>Reading.Record</c>).
/// </summary>
public sealed class PoolSession : AggregateRoot<PoolSessionId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FacilityId FacilityId { get; private set; }
    public FlatId FlatId { get; private set; }
    public PoolPersonType PersonType { get; private set; }
    public PoolAgeCategory AgeCategory { get; private set; }
    public PoolSessionId? AccompaniedBySessionId { get; private set; }
    public DateTimeOffset EntryAtUtc { get; private set; }
    public DateTimeOffset? ExitAtUtc { get; private set; }
    public decimal? GuestFeeAmount { get; private set; }
    public DateTimeOffset? SafetyAcknowledgedAtUtc { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    public Guid? CheckedOutBy { get; private set; }
    public string? OverrideReason { get; private set; }
    public PoolSessionStatus Status { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private PoolSession() { }

    private PoolSession(
        PoolSessionId id,
        Guid tenantId,
        FacilityId facilityId,
        FlatId flatId,
        PoolPersonType personType,
        PoolAgeCategory ageCategory,
        PoolSessionId? accompaniedBySessionId,
        decimal? guestFeeAmount,
        DateTimeOffset? safetyAcknowledgedAtUtc,
        Guid? checkedInBy,
        string? overrideReason,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FacilityId = facilityId;
        FlatId = flatId;
        PersonType = personType;
        AgeCategory = ageCategory;
        AccompaniedBySessionId = accompaniedBySessionId;
        EntryAtUtc = nowUtc;
        GuestFeeAmount = guestFeeAmount;
        SafetyAcknowledgedAtUtc = safetyAcknowledgedAtUtc;
        CheckedInBy = checkedInBy;
        OverrideReason = overrideReason;
        Status = PoolSessionStatus.CheckedIn;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static PoolSession CheckIn(
        Guid tenantId,
        FacilityId facilityId,
        FlatId flatId,
        PoolPersonType personType,
        PoolAgeCategory ageCategory,
        PoolSessionId? accompaniedBySessionId,
        decimal? guestFeeAmount,
        DateTimeOffset? safetyAcknowledgedAtUtc,
        Guid? checkedInBy,
        string? overrideReason,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (guestFeeAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guestFeeAmount), "GuestFeeAmount cannot be negative.");
        }

        return new PoolSession(
            PoolSessionId.New(), tenantId, facilityId, flatId, personType, ageCategory, accompaniedBySessionId,
            guestFeeAmount, safetyAcknowledgedAtUtc, checkedInBy, overrideReason, nowUtc);
    }

    public void CheckOut(Guid? checkedOutBy, DateTimeOffset nowUtc)
    {
        if (Status == PoolSessionStatus.CheckedOut)
        {
            throw new PoolSessionAlreadyClosedException(Id);
        }

        ExitAtUtc = nowUtc;
        CheckedOutBy = checkedOutBy;
        Status = PoolSessionStatus.CheckedOut;
        Version++;
    }
}
