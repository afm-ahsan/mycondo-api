using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Ledger;

/// <summary>
/// One line of a balanced <see cref="LedgerPosting"/> — append-only, never updated or deleted.
/// Corrections are made by posting new reversing entries that reference the original posting, never
/// by editing a posted entry (see <c>financial-engine.md</c>'s "posted financial records are
/// immutable" rule). Only created via <see cref="LedgerPosting.Create"/>, which is what enforces
/// debits == credits — there is no public constructor that bypasses that check.
/// </summary>
public sealed class LedgerEntry : Entity<LedgerEntryId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public LedgerPostingId PostingId { get; private set; }
    public LedgerAccountType AccountType { get; private set; }
    public FlatId? FlatId { get; private set; }
    public LedgerDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Resolved general-ledger account for this line — populated by the Application-layer
    /// centralized posting service via account-mapping resolution (ADR-027). Null for entries posted
    /// before the Finance foundation existed; every entry posted through the centralized service going
    /// forward has this set.</summary>
    public ChartOfAccountId? ChartOfAccountId { get; private set; }

    /// <summary>Optional fund attribution — no existing posting role requires one yet.</summary>
    public FundId? FundId { get; private set; }

    /// <summary>The accounting period covering <see cref="BusinessDate"/>, if one was configured at
    /// posting time. Null for entries posted before periods existed, or when no period covers the
    /// business date — not fabricated retroactively for historical rows (ADR-027).</summary>
    public AccountingPeriodId? AccountingPeriodId { get; private set; }

    private LedgerEntry()
    {
        Description = null!;
    }

    internal LedgerEntry(
        LedgerEntryId id,
        Guid tenantId,
        LedgerPostingId postingId,
        LedgerAccountType accountType,
        FlatId? flatId,
        LedgerDirection direction,
        decimal amount,
        DateOnly businessDate,
        string description,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        PostingId = postingId;
        AccountType = accountType;
        FlatId = flatId;
        Direction = direction;
        Amount = amount;
        BusinessDate = businessDate;
        Description = description;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Stamps the Finance-foundation dimensions resolved by the Application-layer centralized
    /// posting service — called once, immediately after construction, before the entry is added to the
    /// change tracker. Not exposed as part of <see cref="LedgerPosting.Create"/>'s signature so that
    /// method's existing, already-tested balance/immutability invariant stays untouched (ADR-027).</summary>
    public void SetFinanceDimensions(ChartOfAccountId? chartOfAccountId, FundId? fundId, AccountingPeriodId? accountingPeriodId)
    {
        ChartOfAccountId = chartOfAccountId;
        FundId = fundId;
        AccountingPeriodId = accountingPeriodId;
    }
}
