using MyCondo.Domain.Common;
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
}
