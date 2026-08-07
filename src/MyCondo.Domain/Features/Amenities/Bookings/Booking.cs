using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Bookings.Exceptions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Amenities.Bookings;

/// <summary>
/// One Community Hall (or other bookable, non-pool facility) reservation, walking
/// Draft → [PendingApproval] → [AwaitingPayment] → Confirmed → CheckedIn → Completed →
/// ClosedAfterInspection, with Cancelled/Rejected/NoShow as early exits. Charge/deposit amounts and
/// the cancellation policy are snapshotted from <see cref="Facility"/> at <see cref="Request"/> so a
/// later facility-configuration change never retroactively alters an in-flight or historical booking.
/// Booking-vs-booking overlap is enforced by a partial <c>EXCLUDE USING gist</c> constraint at the
/// persistence layer (see plan §3) covering every status except Cancelled/Rejected/NoShow — a slot is
/// held from submission, not just confirmation, so two residents can't both hold the same window while
/// one's approval is pending.
/// </summary>
public sealed class Booking : AggregateRoot<BookingId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FacilityId FacilityId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public FlatId FlatId { get; private set; }
    public string EventType { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public int SetupBufferMinutes { get; private set; }
    public int CleanupBufferMinutes { get; private set; }
    public int ExpectedGuestCount { get; private set; }
    public decimal BookingChargeAmount { get; private set; }
    public decimal DepositAmount { get; private set; }
    public int CancellationDeadlineHours { get; private set; }
    public decimal CancellationDeductionPercentage { get; private set; }
    public bool ApprovalRequired { get; private set; }
    public bool PaymentRequired { get; private set; }
    public BookingStatus Status { get; private set; }
    public InvoiceId? InvoiceId { get; private set; }
    public LedgerPostingId? DepositCollectionPostingId { get; private set; }
    public LedgerPostingId? DepositSettlementPostingId { get; private set; }
    public decimal? DepositRefundedAmount { get; private set; }
    public decimal? DepositDeductedAmount { get; private set; }
    public DateTimeOffset? TermsAcceptedAtUtc { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? RejectedReason { get; private set; }
    public string? CancelledReason { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    public DateTimeOffset? CheckedInAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Guid? InspectedBy { get; private set; }
    public DateTimeOffset? InspectedAtUtc { get; private set; }
    public string? InspectionNotes { get; private set; }
    public string? DamageDeductionReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Booking()
    {
        EventType = null!;
    }

    private Booking(
        BookingId id,
        Guid tenantId,
        FacilityId facilityId,
        BuildingId buildingId,
        FlatId flatId,
        string eventType,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        int setupBufferMinutes,
        int cleanupBufferMinutes,
        int expectedGuestCount,
        bool approvalRequired,
        decimal bookingChargeAmount,
        decimal depositAmount,
        int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage,
        DateTimeOffset? termsAcceptedAtUtc,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FacilityId = facilityId;
        BuildingId = buildingId;
        FlatId = flatId;
        EventType = eventType;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        SetupBufferMinutes = setupBufferMinutes;
        CleanupBufferMinutes = cleanupBufferMinutes;
        ExpectedGuestCount = expectedGuestCount;
        BookingChargeAmount = bookingChargeAmount;
        DepositAmount = depositAmount;
        CancellationDeadlineHours = cancellationDeadlineHours;
        CancellationDeductionPercentage = cancellationDeductionPercentage;
        ApprovalRequired = approvalRequired;
        PaymentRequired = bookingChargeAmount > 0 || depositAmount > 0;
        Status = BookingStatus.Draft;
        TermsAcceptedAtUtc = termsAcceptedAtUtc;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Validates the pure, context-free invariants (guest count vs. positive amounts). The
    /// cross-aggregate checks — facility capacity/active status, blackout-date overlap — need
    /// repository lookups and are enforced by the Application-layer handler before this is called, not
    /// here (same division of responsibility as <c>Reading.Record</c>).</summary>
    public static Booking Request(
        Guid tenantId,
        FacilityId facilityId,
        BuildingId buildingId,
        FlatId flatId,
        string eventType,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        int setupBufferMinutes,
        int cleanupBufferMinutes,
        int expectedGuestCount,
        bool approvalRequired,
        decimal bookingChargeAmount,
        decimal depositAmount,
        int cancellationDeadlineHours,
        decimal cancellationDeductionPercentage,
        DateTimeOffset? termsAcceptedAtUtc,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (endAtUtc <= startAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(endAtUtc), "EndAtUtc must be after StartAtUtc.");
        }

        if (expectedGuestCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedGuestCount), "ExpectedGuestCount must be positive.");
        }

        if (setupBufferMinutes < 0 || cleanupBufferMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(setupBufferMinutes), "Buffers cannot be negative.");
        }

        if (bookingChargeAmount < 0 || depositAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingChargeAmount), "Charge and deposit amounts cannot be negative.");
        }

        return new Booking(
            BookingId.New(), tenantId, facilityId, buildingId, flatId, eventType.Trim(), startAtUtc, endAtUtc,
            setupBufferMinutes, cleanupBufferMinutes, expectedGuestCount, approvalRequired, bookingChargeAmount,
            depositAmount, cancellationDeadlineHours, cancellationDeductionPercentage, termsAcceptedAtUtc, nowUtc);
    }

    public void Submit(DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.Draft)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be submitted");
        }

        Status = ApprovalRequired
            ? BookingStatus.PendingApproval
            : PaymentRequired ? BookingStatus.AwaitingPayment : BookingStatus.Confirmed;
        Version++;
    }

    public void Approve(Guid? approvedBy, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.PendingApproval)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be approved");
        }

        ApprovedBy = approvedBy;
        ApprovedAtUtc = nowUtc;
        Status = PaymentRequired ? BookingStatus.AwaitingPayment : BookingStatus.Confirmed;
        Version++;
    }

    public void Reject(string reason, Guid? rejectedBy, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.PendingApproval)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be rejected");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RejectedReason = reason.Trim();
        Status = BookingStatus.Rejected;
        Version++;
    }

    /// <summary>Handler bills the booking-charge invoice and/or posts the deposit-collection ledger
    /// entry before calling this — see plan §5.</summary>
    public void ConfirmPayment(InvoiceId? invoiceId, LedgerPostingId? depositCollectionPostingId, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.AwaitingPayment)
        {
            throw new BookingInvalidTransitionException(Id, Status, "have payment confirmed");
        }

        InvoiceId = invoiceId;
        DepositCollectionPostingId = depositCollectionPostingId;
        Status = BookingStatus.Confirmed;
        Version++;
    }

    public void CheckIn(Guid? checkedInBy, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be checked in");
        }

        CheckedInBy = checkedInBy;
        CheckedInAtUtc = nowUtc;
        Status = BookingStatus.CheckedIn;
        Version++;
    }

    public void Complete(DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.CheckedIn)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be completed");
        }

        CompletedAtUtc = nowUtc;
        Status = BookingStatus.Completed;
        Version++;
    }

    /// <summary>Handler posts the deposit-settlement ledger entry (if a deposit was collected) before
    /// calling this — see plan §5.</summary>
    public void Inspect(
        Guid? inspectedBy,
        string? notes,
        string? damageDeductionReason,
        decimal? refundedAmount,
        decimal? deductedAmount,
        LedgerPostingId? settlementPostingId,
        DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.Completed)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be inspected");
        }

        InspectedBy = inspectedBy;
        InspectedAtUtc = nowUtc;
        InspectionNotes = notes?.Trim();
        DamageDeductionReason = damageDeductionReason?.Trim();
        DepositRefundedAmount = refundedAmount;
        DepositDeductedAmount = deductedAmount;
        DepositSettlementPostingId = settlementPostingId;
        Status = BookingStatus.ClosedAfterInspection;
        Version++;
    }

    /// <summary>Valid from any pre-check-in status. If a deposit was already collected
    /// (<see cref="DepositCollectionPostingId"/> set), the handler computes the refund/forfeiture split
    /// per the snapshotted cancellation policy and posts the settlement ledger entry before calling
    /// this — see plan §5.</summary>
    public void Cancel(
        string reason, Guid? cancelledBy, decimal? refundedAmount, decimal? deductedAmount,
        LedgerPostingId? settlementPostingId, DateTimeOffset nowUtc)
    {
        if (Status is not (BookingStatus.Draft or BookingStatus.PendingApproval or BookingStatus.AwaitingPayment or BookingStatus.Confirmed))
        {
            throw new BookingInvalidTransitionException(Id, Status, "be cancelled");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        CancelledReason = reason.Trim();
        CancelledBy = cancelledBy;
        CancelledAtUtc = nowUtc;
        if (refundedAmount is not null || deductedAmount is not null)
        {
            DepositRefundedAmount = refundedAmount;
            DepositDeductedAmount = deductedAmount;
            DepositSettlementPostingId = settlementPostingId;
        }

        Status = BookingStatus.Cancelled;
        Version++;
    }

    /// <summary>Treated as always within the cancellation deadline — the full
    /// <see cref="CancellationDeductionPercentage"/> of any collected deposit is forfeited.</summary>
    public void MarkNoShow(decimal? refundedAmount, decimal? deductedAmount, LedgerPostingId? settlementPostingId, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new BookingInvalidTransitionException(Id, Status, "be marked no-show");
        }

        if (refundedAmount is not null || deductedAmount is not null)
        {
            DepositRefundedAmount = refundedAmount;
            DepositDeductedAmount = deductedAmount;
            DepositSettlementPostingId = settlementPostingId;
        }

        Status = BookingStatus.NoShow;
        Version++;
    }
}
