using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.BlackoutDates.Exceptions;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Domain.Features.Amenities.BlackoutDates;

/// <summary>
/// Blocks a <see cref="Facility"/> over a date range — a maintenance closure, holiday, or other
/// exclusion window. Reused for both Community Hall closures and Swimming Pool maintenance closures
/// (same concept, no duplicate "closure" type per facility category). Checked as an extra guard at
/// booking-request time and pool check-in time, on top of <c>Bookings.Booking</c>'s own DB overlap
/// constraint, which only prevents booking-vs-booking overlap, not booking-vs-blackout.
/// </summary>
public sealed class BlackoutDate : Entity<BlackoutDateId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FacilityId FacilityId { get; private set; }
    public DateOnly DateFrom { get; private set; }
    public DateOnly DateTo { get; private set; }
    public string Reason { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private BlackoutDate()
    {
        Reason = null!;
    }

    private BlackoutDate(
        BlackoutDateId id, Guid tenantId, FacilityId facilityId, DateOnly dateFrom, DateOnly dateTo, string reason,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FacilityId = facilityId;
        DateFrom = dateFrom;
        DateTo = dateTo;
        Reason = reason;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public static BlackoutDate Create(
        Guid tenantId, FacilityId facilityId, DateOnly dateFrom, DateOnly dateTo, string reason, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (dateTo < dateFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(dateTo), "DateTo cannot precede DateFrom.");
        }

        return new BlackoutDate(BlackoutDateId.New(), tenantId, facilityId, dateFrom, dateTo, reason.Trim(), nowUtc);
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new BlackoutDateAlreadyInactiveException(Id);
        }

        IsActive = false;
    }

    public bool Covers(DateOnly date) => IsActive && date >= DateFrom && date <= DateTo;
}
