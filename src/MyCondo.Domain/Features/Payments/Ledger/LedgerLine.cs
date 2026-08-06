using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Ledger;

/// <summary>Input to <see cref="LedgerPosting.Create"/> — not persisted itself; each line becomes a
/// <see cref="LedgerEntry"/> once the posting balance check passes.</summary>
public sealed record LedgerLine(
    LedgerAccountType AccountType,
    FlatId? FlatId,
    LedgerDirection Direction,
    decimal Amount,
    string Description);
