using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger.Exceptions;

namespace MyCondo.Domain.Features.Payments.Ledger;

/// <summary>
/// Groups a balanced set of <see cref="LedgerEntry"/> lines into one financial transaction (a
/// charge, a payment, a reversal, a migrated opening balance). <see cref="Create"/> is the only way
/// to produce entries — it is what enforces "every journal entry balances (debits == credits)" in
/// the aggregate itself, not by handler-level convention. Immutable once created; corrections post a
/// brand-new <see cref="LedgerPosting"/> with reversing lines that reference this one via
/// <see cref="ReferenceType"/>/<see cref="ReferenceId"/> — never edited in place.
/// </summary>
public sealed class LedgerPosting : AggregateRoot<LedgerPostingId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string Description { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTimeOffset PostedAtUtc { get; private set; }

    private LedgerPosting()
    {
        Description = null!;
    }

    private LedgerPosting(
        LedgerPostingId id,
        Guid tenantId,
        DateOnly businessDate,
        string description,
        string? referenceType,
        Guid? referenceId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BusinessDate = businessDate;
        Description = description;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        PostedAtUtc = nowUtc;
    }

    /// <summary>Validates that <paramref name="lines"/> balance (debits == credits) and that only
    /// <see cref="LedgerAccountType.ResidentReceivable"/> lines carry a FlatId, then materializes the
    /// posting plus its entries. Throws <see cref="LedgerPostingUnbalancedException"/> if the lines
    /// don't balance — callers cannot construct an unbalanced posting.</summary>
    public static (LedgerPosting Posting, IReadOnlyList<LedgerEntry> Entries) Create(
        Guid tenantId,
        DateOnly businessDate,
        string description,
        string? referenceType,
        Guid? referenceId,
        IReadOnlyList<LedgerLine> lines,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (lines.Count < 2)
        {
            throw new ArgumentException("A ledger posting needs at least two lines to balance.", nameof(lines));
        }

        foreach (LedgerLine line in lines)
        {
            if (line.Amount <= 0)
            {
                throw new ArgumentException("Every ledger line amount must be positive.", nameof(lines));
            }

            bool hasFlatId = line.FlatId is not null;
            bool requiresFlatId = line.AccountType == LedgerAccountType.ResidentReceivable;
            if (hasFlatId != requiresFlatId)
            {
                throw new ArgumentException(
                    $"{line.AccountType} lines must {(requiresFlatId ? "" : "not ")}carry a FlatId.", nameof(lines));
            }
        }

        decimal totalDebits = lines.Where(l => l.Direction == LedgerDirection.Debit).Sum(l => l.Amount);
        decimal totalCredits = lines.Where(l => l.Direction == LedgerDirection.Credit).Sum(l => l.Amount);
        if (totalDebits != totalCredits)
        {
            throw new LedgerPostingUnbalancedException(totalDebits, totalCredits);
        }

        LedgerPostingId postingId = LedgerPostingId.New();
        LedgerPosting posting = new(postingId, tenantId, businessDate, description.Trim(), referenceType, referenceId, nowUtc);

        List<LedgerEntry> entries = lines
            .Select(line => new LedgerEntry(
                LedgerEntryId.New(), tenantId, postingId, line.AccountType, line.FlatId, line.Direction,
                line.Amount, businessDate, line.Description, nowUtc))
            .ToList();

        return (posting, entries);
    }
}
