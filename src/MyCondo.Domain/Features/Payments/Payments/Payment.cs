using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments.Exceptions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Payments;

/// <summary>
/// A resident payment record — always paired 1:1 with the <see cref="LedgerPosting"/> that recorded
/// its ledger effect (Debit CashOrBank, Credit ResidentReceivable). Reversal marks this record
/// Reversed and is expected to be paired with a second, opposite <see cref="LedgerPosting"/> — this
/// entity never re-posts or mutates the original posting itself (posted financial records are
/// immutable).
/// </summary>
public sealed class Payment : AggregateRoot<PaymentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FlatId FlatId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public PaymentStatus Status { get; private set; }
    public LedgerPostingId LedgerPostingId { get; private set; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Payment() { }

    private Payment(
        PaymentId id,
        Guid tenantId,
        FlatId flatId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? referenceNumber,
        DateOnly businessDate,
        Guid? receivedBy,
        LedgerPostingId ledgerPostingId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FlatId = flatId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        ReferenceNumber = referenceNumber;
        BusinessDate = businessDate;
        ReceivedBy = receivedBy;
        Status = PaymentStatus.Posted;
        LedgerPostingId = ledgerPostingId;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Payment Record(
        Guid tenantId,
        FlatId flatId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? referenceNumber,
        DateOnly businessDate,
        Guid? receivedBy,
        LedgerPostingId ledgerPostingId,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        return new Payment(
            PaymentId.New(), tenantId, flatId, amount, paymentMethod, referenceNumber?.Trim(), businessDate,
            receivedBy, ledgerPostingId, nowUtc);
    }

    public void Reverse(string reason, Guid? reversedBy, DateTimeOffset nowUtc)
    {
        if (Status == PaymentStatus.Reversed)
        {
            throw new PaymentAlreadyReversedException(Id);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = PaymentStatus.Reversed;
        ReversedAtUtc = nowUtc;
        ReversedBy = reversedBy;
        ReversalReason = reason.Trim();
        Version++;
    }
}
