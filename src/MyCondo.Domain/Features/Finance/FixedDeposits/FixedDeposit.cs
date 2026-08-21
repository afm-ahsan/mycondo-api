using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits.Exceptions;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>
/// A Fixed Deposit instrument (Template 4) — the Association's actual FD certificates, not a single
/// aggregate "Total FD" balance. Owns the workflow/source record; <c>IFinancialPostingService</c> owns
/// the resulting <c>LedgerPosting</c>s (same "aggregate != journal entry" boundary Template 3's
/// <see cref="Expenses.Expenses.Expense"/> established). Principal movement is an asset transfer, never
/// income/expense — see <see cref="Place"/>/<see cref="MarkRenewed"/>/<see cref="MarkWithdrawn"/>'s
/// posting-purpose doc comments in the command handlers that call them.
/// </summary>
public sealed class FixedDeposit : AggregateRoot<FixedDepositId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string CertificateNumber { get; private set; }
    public string BankName { get; private set; }
    public string? BranchName { get; private set; }

    /// <summary>The account the placement principal was drawn from. Snapshot fields
    /// (<see cref="BankName"/>/<see cref="BranchName"/>) are captured independently at placement time so
    /// this FD's historical bank attribution survives the <see cref="FinancialAccount"/> being later
    /// renamed/deactivated/replaced.</summary>
    public FinancialAccountId FundingFinancialAccountId { get; private set; }

    /// <summary>Set once this FD matures/is withdrawn — the account principal was returned to. Usually
    /// the same as <see cref="FundingFinancialAccountId"/> but not required to be.</summary>
    public FinancialAccountId? ReceivingFinancialAccountId { get; private set; }

    public FundId? FundId { get; private set; }
    public decimal Principal { get; private set; }
    public decimal InterestRatePercent { get; private set; }
    public InterestCalculationMethod CalculationMethod { get; private set; }
    public InterestPaymentFrequency PaymentFrequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly MaturityDate { get; private set; }
    public decimal? ExpectedGrossInterest { get; private set; }

    /// <summary>Configurable deduction-at-source rate (e.g. Bangladesh AIT) applied when interest is
    /// actually received — metadata only; never hard-coded into accrual/receipt posting logic (Template
    /// 4's "do not hard-code Bangladesh tax treatment" requirement). Null means no deduction is expected.
    /// </summary>
    public decimal? ExpectedDeductionRatePercent { get; private set; }

    public string? Notes { get; private set; }
    public FixedDepositStatus Status { get; private set; }

    /// <summary>Renewal lineage — set on the successor when created via <see cref="PlaceAsRenewal"/>.
    /// Never overwritten; a matured FD's history is never edited, only superseded (Template 4's "never
    /// overwrite a matured FD" requirement).</summary>
    public FixedDepositId? PredecessorFixedDepositId { get; private set; }

    /// <summary>Set on the predecessor by <see cref="MarkRenewed"/> once its successor exists.</summary>
    public FixedDepositId? SuccessorFixedDepositId { get; private set; }

    /// <summary>The posting that established this instrument's own principal balance — set for a
    /// brand-new placement (<see cref="Place"/>). For a renewal successor (<see cref="PlaceAsRenewal"/>)
    /// this is null unless the renewed principal actually differs from the predecessor's (see
    /// <see cref="RenewalAdjustmentPostingId"/>) — a pure lineage-continuation renewal posts nothing of
    /// its own; the principal balance still rests on the original placement's entry, which the
    /// tenant-wide <c>LedgerAccountType.FixedDeposit</c> account never needed to adjust.</summary>
    public LedgerPostingId? PlacementPostingId { get; private set; }

    /// <summary>Only set when renewal principal differs from the predecessor's (capitalized interest
    /// rolled in, or a partial withdrawal taken at renewal) — see <c>RenewFixedDepositCommandHandler</c>.
    /// Null when the renewed principal is unchanged (pure lineage continuation, no ledger effect).
    /// Mirrors <see cref="PlacementPostingId"/> when set — same posting, referenced from both sides of
    /// the lineage.</summary>
    public LedgerPostingId? RenewalAdjustmentPostingId { get; private set; }

    public LedgerPostingId? WithdrawalPostingId { get; private set; }
    public string? VoidReason { get; private set; }
    public LedgerPostingId? VoidReversalPostingId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FixedDeposit()
    {
        CertificateNumber = null!;
        BankName = null!;
    }

    private FixedDeposit(
        FixedDepositId id, Guid tenantId, string certificateNumber, string bankName, string? branchName,
        FinancialAccountId fundingFinancialAccountId, FundId? fundId, decimal principal,
        decimal interestRatePercent, InterestCalculationMethod calculationMethod,
        InterestPaymentFrequency paymentFrequency, DateOnly startDate, DateOnly maturityDate,
        decimal? expectedGrossInterest, decimal? expectedDeductionRatePercent, string? notes,
        FixedDepositId? predecessorFixedDepositId, LedgerPostingId? placementPostingId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        CertificateNumber = certificateNumber;
        BankName = bankName;
        BranchName = branchName;
        FundingFinancialAccountId = fundingFinancialAccountId;
        FundId = fundId;
        Principal = principal;
        InterestRatePercent = interestRatePercent;
        CalculationMethod = calculationMethod;
        PaymentFrequency = paymentFrequency;
        StartDate = startDate;
        MaturityDate = maturityDate;
        ExpectedGrossInterest = expectedGrossInterest;
        ExpectedDeductionRatePercent = expectedDeductionRatePercent;
        Notes = notes;
        PredecessorFixedDepositId = predecessorFixedDepositId;
        PlacementPostingId = placementPostingId;
        Status = FixedDepositStatus.Active;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Creates a brand-new (non-renewal) Fixed Deposit, already carrying the posting id of its
    /// placement — <paramref name="id"/> and <paramref name="placementPostingId"/> are supplied by the
    /// command handler because the placement posting's <c>SourceId</c> must be this FD's id, so the id is
    /// generated before this factory runs and the resulting posting id is threaded back in immediately
    /// after (same "post first, then construct the source record" ordering <c>ApproveExpenseCommandHandler</c>
    /// uses, adapted since here creation and posting are one atomic action, not two).</summary>
    public static FixedDeposit Place(
        FixedDepositId id, Guid tenantId, string certificateNumber, string bankName, string? branchName,
        FinancialAccountId fundingFinancialAccountId, FundId? fundId, decimal principal,
        decimal interestRatePercent, InterestCalculationMethod calculationMethod,
        InterestPaymentFrequency paymentFrequency, DateOnly startDate, DateOnly maturityDate,
        decimal? expectedGrossInterest, decimal? expectedDeductionRatePercent, string? notes,
        LedgerPostingId placementPostingId, DateTimeOffset nowUtc)
    {
        ValidateCoreTerms(
            tenantId, certificateNumber, bankName, principal, interestRatePercent, startDate, maturityDate);

        return new FixedDeposit(
            id, tenantId, certificateNumber.Trim(), bankName.Trim(), branchName?.Trim(),
            fundingFinancialAccountId, fundId, principal, interestRatePercent, calculationMethod,
            paymentFrequency, startDate, maturityDate, expectedGrossInterest, expectedDeductionRatePercent,
            notes?.Trim(), predecessorFixedDepositId: null, placementPostingId, nowUtc);
    }

    /// <summary>Creates the successor of a renewed Fixed Deposit — see <see cref="MarkRenewed"/>, called
    /// on the predecessor by the same command handler immediately after this factory.
    /// <paramref name="renewalAdjustmentPostingId"/> is null for a pure lineage-continuation renewal
    /// (unchanged principal, no posting of its own) — see <see cref="PlacementPostingId"/>'s doc comment.
    /// </summary>
    public static FixedDeposit PlaceAsRenewal(
        FixedDepositId id, FixedDeposit predecessor, string certificateNumber, string? branchName,
        FinancialAccountId fundingFinancialAccountId, decimal principal, decimal interestRatePercent,
        InterestCalculationMethod calculationMethod, InterestPaymentFrequency paymentFrequency,
        DateOnly startDate, DateOnly maturityDate, decimal? expectedGrossInterest,
        decimal? expectedDeductionRatePercent, string? notes, LedgerPostingId? renewalAdjustmentPostingId,
        DateTimeOffset nowUtc)
    {
        ValidateCoreTerms(
            predecessor.TenantId, certificateNumber, predecessor.BankName, principal, interestRatePercent,
            startDate, maturityDate);

        return new FixedDeposit(
            id, predecessor.TenantId, certificateNumber.Trim(), predecessor.BankName, branchName?.Trim(),
            fundingFinancialAccountId, predecessor.FundId, principal, interestRatePercent, calculationMethod,
            paymentFrequency, startDate, maturityDate, expectedGrossInterest, expectedDeductionRatePercent,
            notes?.Trim(), predecessor.Id, renewalAdjustmentPostingId, nowUtc);
    }

    private static void ValidateCoreTerms(
        Guid tenantId, string certificateNumber, string bankName, decimal principal, decimal interestRatePercent,
        DateOnly startDate, DateOnly maturityDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (principal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(principal), principal, "Principal must be greater than zero.");
        }

        if (interestRatePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(interestRatePercent), interestRatePercent, "Interest rate cannot be negative.");
        }

        if (maturityDate <= startDate)
        {
            throw new ArgumentException("MaturityDate must be after StartDate.", nameof(maturityDate));
        }
    }

    /// <summary>Notes are the only field editable after placement — financial terms are immutable once
    /// posted (root CLAUDE.md's "do not mutate posted financial ledger entries"); a data-entry mistake in
    /// the terms is corrected by <see cref="Void"/> (while still un-actioned) and re-placing, never by
    /// editing a placed FD's terms in place.</summary>
    public void UpdateNotes(string? notes, DateTimeOffset nowUtc)
    {
        Notes = notes?.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Links this (predecessor) FD to its successor — called immediately after
    /// <see cref="PlaceAsRenewal"/> creates the successor, with that call's resulting id.
    /// <paramref name="renewalAdjustmentPostingId"/> is only non-null when the renewed principal differs
    /// from this FD's own (capitalized interest, or a partial withdrawal taken at renewal).</summary>
    public void MarkRenewed(FixedDepositId successorFixedDepositId, LedgerPostingId? renewalAdjustmentPostingId, DateTimeOffset nowUtc)
    {
        if (Status != FixedDepositStatus.Active)
        {
            throw new FixedDepositInvalidStateTransitionException(Id, Status, "be renewed");
        }

        SuccessorFixedDepositId = successorFixedDepositId;
        RenewalAdjustmentPostingId = renewalAdjustmentPostingId;
        Status = FixedDepositStatus.Renewed;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Matured/early withdrawal — principal returned to <paramref name="receivingFinancialAccountId"/>,
    /// terminal, not a renewal. Called after the "Dr Bank / Cr FixedDeposit" posting succeeds.</summary>
    public void MarkWithdrawn(LedgerPostingId withdrawalPostingId, FinancialAccountId receivingFinancialAccountId, DateTimeOffset nowUtc)
    {
        if (Status != FixedDepositStatus.Active)
        {
            throw new FixedDepositInvalidStateTransitionException(Id, Status, "be withdrawn");
        }

        WithdrawalPostingId = withdrawalPostingId;
        ReceivingFinancialAccountId = receivingFinancialAccountId;
        Status = FixedDepositStatus.Withdrawn;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Voids a placement made in error — only while still <see cref="FixedDepositStatus.Active"/>.
    /// The command handler enforces the additional "no interest activity posted yet" guard (spans the
    /// separate <c>FixedDepositInterestAccrual</c>/<c>FixedDepositInterestReceipt</c> aggregates, so it
    /// cannot live here) before posting <paramref name="reversalPostingId"/> and calling this.</summary>
    public void Void(string reason, LedgerPostingId reversalPostingId, DateTimeOffset nowUtc)
    {
        if (Status != FixedDepositStatus.Active)
        {
            throw new FixedDepositInvalidStateTransitionException(Id, Status, "be voided");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        VoidReason = reason.Trim();
        VoidReversalPostingId = reversalPostingId;
        Status = FixedDepositStatus.Voided;
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
