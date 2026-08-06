using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings.Exceptions;

namespace MyCondo.Domain.Features.Utilities.Readings;

/// <summary>
/// One meter-reading, walking Draft → Reviewed → Finalized → Billed. No edit method exists at any
/// status — a wrong Draft/Reviewed reading is simply abandoned (left un-finalized, harmless) and
/// re-recorded; a wrong Finalized-or-Billed reading is corrected by recording a brand-new
/// <see cref="Reading"/> with <see cref="CorrectsReadingId"/> set (see <see cref="MarkCorrected"/>),
/// which restarts the full cycle — a deliberate "controlled revision process," not an instant fix.
/// </summary>
public sealed class Reading : AggregateRoot<ReadingId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public MeterId MeterId { get; private set; }
    public FlatId FlatId { get; private set; }
    public UtilityType UtilityType { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal PreviousReading { get; private set; }
    public decimal PresentReading { get; private set; }
    public decimal ConsumptionUnits { get; private set; }
    public DateOnly ReadingDate { get; private set; }
    public string? OverrideReason { get; private set; }
    public bool IsAbnormalConsumption { get; private set; }
    public string? AbnormalConsumptionReason { get; private set; }
    public ReadingStatus Status { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public DateTimeOffset? BilledAtUtc { get; private set; }
    public Guid? BilledBy { get; private set; }
    public InvoiceId? InvoiceId { get; private set; }
    public ReadingId? CorrectsReadingId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Reading() { }

    private Reading(
        ReadingId id,
        Guid tenantId,
        MeterId meterId,
        FlatId flatId,
        UtilityType utilityType,
        BuildingId buildingId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal previousReading,
        decimal presentReading,
        DateOnly readingDate,
        string? overrideReason,
        ReadingId? correctsReadingId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        MeterId = meterId;
        FlatId = flatId;
        UtilityType = utilityType;
        BuildingId = buildingId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PreviousReading = previousReading;
        PresentReading = presentReading;
        ConsumptionUnits = presentReading - previousReading;
        ReadingDate = readingDate;
        OverrideReason = overrideReason;
        Status = ReadingStatus.Draft;
        CorrectsReadingId = correctsReadingId;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Validates the pure, context-free invariant (present &lt; previous needs an override
    /// reason). The cross-reading "previous should match the meter's last finalized present reading"
    /// rule needs a repository lookup and is enforced by the Application-layer handler before this is
    /// called, not here.</summary>
    public static Reading Record(
        Guid tenantId,
        MeterId meterId,
        FlatId flatId,
        UtilityType utilityType,
        BuildingId buildingId,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal previousReading,
        decimal presentReading,
        DateOnly readingDate,
        string? overrideReason,
        ReadingId? correctsReadingId,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (previousReading < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousReading), "PreviousReading cannot be negative.");
        }

        if (presentReading < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(presentReading), "PresentReading cannot be negative.");
        }

        if (presentReading < previousReading && string.IsNullOrWhiteSpace(overrideReason))
        {
            throw new ArgumentException(
                "OverrideReason is required when PresentReading is lower than PreviousReading.", nameof(overrideReason));
        }

        return new Reading(
            ReadingId.New(), tenantId, meterId, flatId, utilityType, buildingId, periodStart, periodEnd,
            previousReading, presentReading, readingDate, overrideReason?.Trim(), correctsReadingId, nowUtc);
    }

    public void Review(Guid? reviewedBy, DateTimeOffset nowUtc)
    {
        if (Status != ReadingStatus.Draft)
        {
            throw new ReadingInvalidTransitionException(Id, Status, "be reviewed");
        }

        Status = ReadingStatus.Reviewed;
        ReviewedAtUtc = nowUtc;
        ReviewedBy = reviewedBy;
        Version++;
    }

    /// <summary>The abnormal-consumption flag/reason are computed by the Application-layer handler
    /// (needs historical readings) and passed in already decided.</summary>
    public void Finalize(Guid? finalizedBy, bool isAbnormalConsumption, string? abnormalConsumptionReason, DateTimeOffset nowUtc)
    {
        if (Status != ReadingStatus.Reviewed)
        {
            throw new ReadingInvalidTransitionException(Id, Status, "be finalized");
        }

        Status = ReadingStatus.Finalized;
        FinalizedAtUtc = nowUtc;
        FinalizedBy = finalizedBy;
        IsAbnormalConsumption = isAbnormalConsumption;
        AbnormalConsumptionReason = abnormalConsumptionReason;
        Version++;
    }

    public void MarkBilled(InvoiceId invoiceId, Guid? billedBy, DateTimeOffset nowUtc)
    {
        if (Status != ReadingStatus.Finalized)
        {
            throw new ReadingInvalidTransitionException(Id, Status, "be billed");
        }

        Status = ReadingStatus.Billed;
        InvoiceId = invoiceId;
        BilledAtUtc = nowUtc;
        BilledBy = billedBy;
        Version++;
    }

    /// <summary>Valid from <see cref="ReadingStatus.Finalized"/> or <see cref="ReadingStatus.Billed"/>
    /// — the caller (handler) is responsible for voiding the associated <c>Invoice</c> first if this
    /// reading was already <see cref="ReadingStatus.Billed"/>.</summary>
    public void MarkCorrected(DateTimeOffset nowUtc)
    {
        if (Status is not (ReadingStatus.Finalized or ReadingStatus.Billed))
        {
            throw new ReadingInvalidTransitionException(Id, Status, "be corrected");
        }

        Status = ReadingStatus.Corrected;
        Version++;
    }
}
