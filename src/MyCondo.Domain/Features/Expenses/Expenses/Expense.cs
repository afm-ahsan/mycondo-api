using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.Expenses.Exceptions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Expenses.Expenses;

/// <summary>
/// A recorded operational expense (Template 3: formalized as an accounting-integrated domain — see
/// <see cref="ExpenseStatus"/> for the lifecycle and root CLAUDE.md's "Expense != Journal Entry" boundary:
/// this aggregate owns the workflow/source record; <c>IFinancialPostingService</c> owns the resulting
/// <c>LedgerPosting</c>). Reuses <see cref="Payments.Payments.PaymentMethod"/> rather than introducing a
/// duplicate enum.
/// </summary>
public sealed class Expense : AggregateRoot<ExpenseId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }

    /// <summary>Null represents an association-wide common expense that isn't scoped to any single
    /// tower/Building — Template 3 explicitly requires this not be forced onto a Building.</summary>
    public BuildingId? BuildingId { get; private set; }
    public ExpenseTypeId ExpenseTypeId { get; private set; }
    public FundId? FundId { get; private set; }

    /// <summary>The date on the source voucher/invoice — when the expense was incurred.</summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>The date used for the ledger posting — defaults to <see cref="ExpenseDate"/> at
    /// recording time but is a distinct field per Template 3's "expense date vs accounting date"
    /// requirement (e.g. a voucher dated in a closed period gets accounted for in the next open one).</summary>
    public DateOnly AccountingDate { get; private set; }

    public string Description { get; private set; }
    public string? Payee { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>True when the expense was paid immediately at recording time ("Dr Expense / Cr
    /// Bank/Cash" posted directly). False means it was recorded unpaid ("Dr Expense / Cr Accounts
    /// Payable"); a later supplier payment posts "Dr Accounts Payable / Cr Bank" — see
    /// <see cref="MarkPaid"/>.</summary>
    public bool IsPaid { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public string? Notes { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public string? VoidReason { get; private set; }

    /// <summary>The Cash/Bank <c>ChartOfAccount</c> the posting service resolved for the Bank/Cash side
    /// of this expense's cash movement — set when the primary posting settles cash immediately
    /// (<see cref="IsPaid"/>) or when a supplier payment is later posted (<see cref="MarkPaid"/>). Never
    /// user-selected: the tenant has one mapped <see cref="LedgerAccountType.CashOrBank"/> account today;
    /// this field exists for traceability/reporting, not multi-account choice.</summary>
    public ChartOfAccountId? FinancialAccountId { get; private set; }

    /// <summary>The primary posting — "Dr Expense / Cr Accounts Payable" (unpaid) or "Dr Expense / Cr
    /// Bank/Cash" (immediate payment) — created by <c>ApproveExpenseCommand</c>.</summary>
    public LedgerPostingId? PostingId { get; private set; }

    /// <summary>The supplier-payment posting ("Dr Accounts Payable / Cr Bank") — only set when this
    /// expense was recorded unpaid and has since been paid via <c>PayExpenseCommand</c>.</summary>
    public LedgerPostingId? PaymentPostingId { get; private set; }

    /// <summary>Reversal of <see cref="PostingId"/>, posted when voiding a <see cref="ExpenseStatus.Posted"/>
    /// or <see cref="ExpenseStatus.Paid"/> expense.</summary>
    public LedgerPostingId? ReversalPostingId { get; private set; }

    /// <summary>Reversal of <see cref="PaymentPostingId"/>, posted only when voiding an expense that had
    /// already reached <see cref="ExpenseStatus.Paid"/>.</summary>
    public LedgerPostingId? PaymentReversalPostingId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Expense()
    {
        Description = null!;
    }

    private Expense(
        ExpenseId id,
        Guid tenantId,
        BuildingId? buildingId,
        ExpenseTypeId expenseTypeId,
        FundId? fundId,
        DateOnly expenseDate,
        DateOnly accountingDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        bool isPaid,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        ExpenseTypeId = expenseTypeId;
        FundId = fundId;
        ExpenseDate = expenseDate;
        AccountingDate = accountingDate;
        Description = description;
        Payee = payee;
        ReferenceNumber = referenceNumber;
        Amount = amount;
        IsPaid = isPaid;
        PaymentMethod = paymentMethod;
        Notes = notes;
        Status = ExpenseStatus.Recorded;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Expense Record(
        Guid tenantId,
        BuildingId? buildingId,
        ExpenseTypeId expenseTypeId,
        FundId? fundId,
        DateOnly expenseDate,
        DateOnly? accountingDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        bool isPaid,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        return new Expense(
            ExpenseId.New(), tenantId, buildingId, expenseTypeId, fundId, expenseDate,
            accountingDate ?? expenseDate, description.Trim(), payee?.Trim(), referenceNumber?.Trim(), amount,
            isPaid, paymentMethod, notes?.Trim(), nowUtc);
    }

    /// <summary>Full edit — only while <see cref="ExpenseStatus.Recorded"/>. Once approved/posted, paid,
    /// or voided, the record is frozen; correct a mistake by voiding (which reverses any posted ledger
    /// consequence) and recording a new one, never by editing a settled row back to "fix" it.</summary>
    public void Update(
        BuildingId? buildingId,
        ExpenseTypeId expenseTypeId,
        FundId? fundId,
        DateOnly expenseDate,
        DateOnly? accountingDate,
        string description,
        string? payee,
        string? referenceNumber,
        decimal amount,
        bool isPaid,
        PaymentMethod paymentMethod,
        string? notes,
        DateTimeOffset nowUtc)
    {
        if (Status != ExpenseStatus.Recorded)
        {
            throw new ExpenseNotEditableException(Id, Status);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        BuildingId = buildingId;
        ExpenseTypeId = expenseTypeId;
        FundId = fundId;
        ExpenseDate = expenseDate;
        AccountingDate = accountingDate ?? expenseDate;
        Description = description.Trim();
        Payee = payee?.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        Amount = amount;
        IsPaid = isPaid;
        PaymentMethod = paymentMethod;
        Notes = notes?.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Approval + primary ledger posting, in one transition (see <see cref="ExpenseStatus"/>'s
    /// doc comment for why these aren't split into separate states). Called by
    /// <c>ApproveExpenseCommandHandler</c> immediately after it successfully posts through
    /// <c>IFinancialPostingService</c> — <paramref name="postingId"/> is that posting's id, never
    /// fabricated here.</summary>
    public void MarkPosted(LedgerPostingId postingId, ChartOfAccountId? financialAccountId, DateTimeOffset nowUtc)
    {
        if (Status != ExpenseStatus.Recorded)
        {
            throw new ExpenseInvalidStateTransitionException(Id, Status, "be approved/posted");
        }

        PostingId = postingId;
        if (financialAccountId is not null)
        {
            FinancialAccountId = financialAccountId;
        }

        Status = ExpenseStatus.Posted;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Records a supplier payment against a previously unpaid, posted expense — only valid from
    /// <see cref="ExpenseStatus.Posted"/> while <see cref="IsPaid"/> is false. Called by
    /// <c>PayExpenseCommandHandler</c> immediately after it posts "Dr Accounts Payable / Cr Bank" through
    /// <c>IFinancialPostingService</c>.</summary>
    public void MarkPaid(LedgerPostingId paymentPostingId, ChartOfAccountId financialAccountId, DateTimeOffset nowUtc)
    {
        if (Status != ExpenseStatus.Posted || IsPaid)
        {
            throw new ExpenseInvalidStateTransitionException(Id, Status, "record a supplier payment");
        }

        PaymentPostingId = paymentPostingId;
        FinancialAccountId = financialAccountId;
        IsPaid = true;
        Status = ExpenseStatus.Paid;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Voids the expense. While still <see cref="ExpenseStatus.Recorded"/> (never posted) this
    /// is a pure status flip, same as pre-Template-3 behaviour. Once <see cref="ExpenseStatus.Posted"/>
    /// or <see cref="ExpenseStatus.Paid"/>, the caller must post the reversing entr(y/ies) through
    /// <c>IFinancialPostingService</c> first and supply the resulting posting id(s) — this method never
    /// posts anything itself (Expense != Journal Entry).</summary>
    public void Void(
        string reason,
        DateTimeOffset nowUtc,
        LedgerPostingId? reversalPostingId = null,
        LedgerPostingId? paymentReversalPostingId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == ExpenseStatus.Voided)
        {
            return;
        }

        if (Status is ExpenseStatus.Posted or ExpenseStatus.Paid && reversalPostingId is null)
        {
            throw new ArgumentException(
                "A posted or paid Expense requires a reversal posting id to void.", nameof(reversalPostingId));
        }

        if (Status == ExpenseStatus.Paid && paymentReversalPostingId is null)
        {
            throw new ArgumentException(
                "A paid Expense requires a payment-reversal posting id to void.", nameof(paymentReversalPostingId));
        }

        ReversalPostingId = reversalPostingId;
        PaymentReversalPostingId = paymentReversalPostingId;
        Status = ExpenseStatus.Voided;
        VoidReason = reason.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
