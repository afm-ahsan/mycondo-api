using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Finance.Services;

/// <summary>One line of a <see cref="FinancialPostingRequest"/> — names the semantic posting role
/// (today, one of the 6 pre-existing <see cref="LedgerAccountType"/> values) rather than an account id;
/// <see cref="IFinancialPostingService"/> resolves the role to the tenant's actual account via
/// <c>AccountMapping</c>. Callers never hard-code an account id (ADR-027).</summary>
public sealed record FinancialPostingLine(
    LedgerAccountType Role,
    FlatId? FlatId,
    LedgerDirection Direction,
    decimal Amount,
    string? LineDescription = null);

/// <summary><paramref name="PostingPurpose"/> and <paramref name="SourceId"/> map directly onto
/// <see cref="LedgerPosting.ReferenceType"/>/<see cref="LedgerPosting.ReferenceId"/> — same meaning,
/// same columns, no rename (ADR-027). <paramref name="SourceId"/> is optional: several existing call
/// sites have no natural per-posting source id and rely on an upstream domain-level dedup check
/// instead (e.g. batch invoice generation) — the database idempotency guard only applies when it's
/// supplied. <paramref name="FundId"/> is optional (added by Template 3) and, when supplied, is stamped
/// onto every resulting <see cref="LedgerEntry"/> via <see cref="LedgerEntry.SetFinanceDimensions"/> —
/// posting-level rather than per-line since every existing/known caller attributes a whole posting to at
/// most one fund.</summary>
public sealed record FinancialPostingRequest(
    Guid TenantId,
    DateOnly BusinessDate,
    string Description,
    string PostingPurpose,
    Guid? SourceId,
    IReadOnlyList<FinancialPostingLine> Lines,
    FundId? FundId = null);

public sealed record FinancialPostingResult(LedgerPosting Posting, IReadOnlyList<LedgerEntry> Entries);

/// <summary>
/// The single centralized entry point for every accounting posting in MyCondo (ADR-027) — nothing else
/// constructs <see cref="LedgerPosting"/>/<see cref="LedgerLine"/> directly outside this service and its
/// own domain tests. Resolves account mappings (fails explicitly rather than posting to a default
/// account), enforces closed-period prevention, delegates to the unmodified
/// <see cref="LedgerPosting.Create"/> for balance/immutability enforcement, and stamps the resolved
/// Finance-foundation dimensions onto the resulting entries.
/// </summary>
public interface IFinancialPostingService
{
    Task<FinancialPostingResult> PostAsync(FinancialPostingRequest request, CancellationToken cancellationToken);
}
