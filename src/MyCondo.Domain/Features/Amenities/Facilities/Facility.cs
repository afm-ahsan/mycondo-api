using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Amenities.Facilities;

/// <summary>
/// Shared master for both Community Hall (bookable) and Swimming Pool (daily-access) facilities,
/// discriminated by <see cref="FacilityType"/> — mirrors the <c>Meter</c> pattern from Slice F rather
/// than two parallel entity sets. Mutable, not effective-dated like <c>ServiceChargeRule</c>: this is
/// a physical/administrative asset, not a rate card. Booking charge/deposit amounts are snapshotted
/// onto <c>Bookings.Booking</c> at request time, so a later <see cref="UpdateConfiguration"/> call
/// never retroactively changes an in-flight or historical booking.
/// </summary>
public sealed class Facility : AggregateRoot<FacilityId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string Name { get; private set; }
    public FacilityType FacilityType { get; private set; }
    public int Capacity { get; private set; }
    public TimeOnly? OperatingHoursStart { get; private set; }
    public TimeOnly? OperatingHoursEnd { get; private set; }
    public bool RequiresApproval { get; private set; }
    public decimal? BookingChargeAmount { get; private set; }
    public decimal? DepositAmount { get; private set; }
    public int CancellationDeadlineHours { get; private set; }
    public decimal CancellationDeductionPercentage { get; private set; }
    public decimal? GuestFeeAmount { get; private set; }
    public int? MinimumAgeUnaccompanied { get; private set; }
    public bool RequiresSafetyAcknowledgement { get; private set; }
    public bool BlocksEntryIfAccountOverdue { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Facility()
    {
        Name = null!;
    }

    private Facility(
        FacilityId id,
        Guid tenantId,
        BuildingId buildingId,
        string name,
        FacilityType facilityType,
        int capacity,
        TimeOnly? operatingHoursStart,
        TimeOnly? operatingHoursEnd,
        bool requiresApproval,
        decimal? bookingChargeAmount,
        decimal? depositAmount,
        int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage,
        decimal? guestFeeAmount,
        int? minimumAgeUnaccompanied,
        bool requiresSafetyAcknowledgement,
        bool blocksEntryIfAccountOverdue,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        Name = name;
        FacilityType = facilityType;
        Capacity = capacity;
        OperatingHoursStart = operatingHoursStart;
        OperatingHoursEnd = operatingHoursEnd;
        RequiresApproval = requiresApproval;
        BookingChargeAmount = bookingChargeAmount;
        DepositAmount = depositAmount;
        CancellationDeadlineHours = cancellationDeadlineHours;
        CancellationDeductionPercentage = cancellationDeductionPercentage;
        GuestFeeAmount = guestFeeAmount;
        MinimumAgeUnaccompanied = minimumAgeUnaccompanied;
        RequiresSafetyAcknowledgement = requiresSafetyAcknowledgement;
        BlocksEntryIfAccountOverdue = blocksEntryIfAccountOverdue;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Facility Create(
        Guid tenantId,
        BuildingId buildingId,
        string name,
        FacilityType facilityType,
        int capacity,
        TimeOnly? operatingHoursStart,
        TimeOnly? operatingHoursEnd,
        bool requiresApproval,
        decimal? bookingChargeAmount,
        decimal? depositAmount,
        int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage,
        decimal? guestFeeAmount,
        int? minimumAgeUnaccompanied,
        bool requiresSafetyAcknowledgement,
        bool blocksEntryIfAccountOverdue,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        ValidateMoneyAndPercentage(
            bookingChargeAmount, depositAmount, cancellationDeadlineHours, cancellationDeductionPercentage, guestFeeAmount);

        return new Facility(
            FacilityId.New(), tenantId, buildingId, name.Trim(), facilityType, capacity, operatingHoursStart,
            operatingHoursEnd, requiresApproval, bookingChargeAmount, depositAmount, cancellationDeadlineHours,
            cancellationDeductionPercentage, guestFeeAmount, minimumAgeUnaccompanied, requiresSafetyAcknowledgement,
            blocksEntryIfAccountOverdue, nowUtc);
    }

    /// <summary>Plain field update, no effective-dating — see the type's doc comment for why this
    /// differs from <c>ServiceChargeRule</c>'s immutable-then-superseded pattern.</summary>
    public void UpdateConfiguration(
        string name,
        int capacity,
        TimeOnly? operatingHoursStart,
        TimeOnly? operatingHoursEnd,
        bool requiresApproval,
        decimal? bookingChargeAmount,
        decimal? depositAmount,
        int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage,
        decimal? guestFeeAmount,
        int? minimumAgeUnaccompanied,
        bool requiresSafetyAcknowledgement,
        bool blocksEntryIfAccountOverdue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        ValidateMoneyAndPercentage(
            bookingChargeAmount, depositAmount, cancellationDeadlineHours, cancellationDeductionPercentage, guestFeeAmount);

        Name = name.Trim();
        Capacity = capacity;
        OperatingHoursStart = operatingHoursStart;
        OperatingHoursEnd = operatingHoursEnd;
        RequiresApproval = requiresApproval;
        BookingChargeAmount = bookingChargeAmount;
        DepositAmount = depositAmount;
        CancellationDeadlineHours = cancellationDeadlineHours;
        CancellationDeductionPercentage = cancellationDeductionPercentage;
        GuestFeeAmount = guestFeeAmount;
        MinimumAgeUnaccompanied = minimumAgeUnaccompanied;
        RequiresSafetyAcknowledgement = requiresSafetyAcknowledgement;
        BlocksEntryIfAccountOverdue = blocksEntryIfAccountOverdue;
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    public void Reactivate()
    {
        IsActive = true;
        Version++;
    }

    private static void ValidateMoneyAndPercentage(
        decimal? bookingChargeAmount, decimal? depositAmount, int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage, decimal? guestFeeAmount)
    {
        if (bookingChargeAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingChargeAmount), "BookingChargeAmount cannot be negative.");
        }

        if (depositAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositAmount), "DepositAmount cannot be negative.");
        }

        if (guestFeeAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guestFeeAmount), "GuestFeeAmount cannot be negative.");
        }

        if (cancellationDeadlineHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cancellationDeadlineHours), "CancellationDeadlineHours cannot be negative.");
        }

        if (cancellationDeductionPercentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cancellationDeductionPercentage), "CancellationDeductionPercentage must be between 0 and 100.");
        }
    }
}
