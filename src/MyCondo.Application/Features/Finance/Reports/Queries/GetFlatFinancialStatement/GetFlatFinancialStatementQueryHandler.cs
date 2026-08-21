using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;

/// <summary>See <see cref="GetFlatFinancialStatementQuery"/>. Running balance uses the same
/// preceding-page re-summation approach as <c>GetAccountLedgerQueryHandler</c> (deep pagination re-sums
/// prior pages rather than caching a balance, honoring "closing balance is always derived from ledger
/// entries, never stored").</summary>
public sealed class GetFlatFinancialStatementQueryHandler(
    IFlatRepository flats,
    ILedgerEntryRepository ledgerEntries,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFlatFinancialStatementQuery, FlatFinancialStatementReportDto>
{
    public async ValueTask<FlatFinancialStatementReportDto> Handle(
        GetFlatFinancialStatementQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(query.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), query.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), query.FlatId);
        }

        decimal openingBalance = query.FromDate is DateOnly fromDate
            ? await ledgerEntries.GetReceivableBalanceForFlatBeforeAsync(tenantId, flatId, fromDate, cancellationToken)
            : 0m;

        (decimal periodDebit, decimal periodCredit) = await ledgerEntries.GetReceivableActivityForFlatAsync(
            tenantId, flatId, query.FromDate, query.ToDate, cancellationToken);

        decimal closingBalance = openingBalance + periodDebit - periodCredit;

        decimal precedingBalance = 0m;
        if (query.Page > 1)
        {
            PagedResult<LedgerEntryWithReference> preceding = await ledgerEntries.SearchForFlatChronologicalAsync(
                tenantId, flatId, query.FromDate, query.ToDate, page: 1, pageSize: (query.Page - 1) * query.PageSize, cancellationToken);
            precedingBalance = preceding.Items.Sum(SignedDelta);
        }

        PagedResult<LedgerEntryWithReference> page = await ledgerEntries.SearchForFlatChronologicalAsync(
            tenantId, flatId, query.FromDate, query.ToDate, query.Page, query.PageSize, cancellationToken);

        decimal runningBalance = openingBalance + precedingBalance;
        List<FlatFinancialStatementLineDto> lines = [];
        foreach (LedgerEntryWithReference x in page.Items)
        {
            runningBalance += SignedDelta(x);
            lines.Add(new FlatFinancialStatementLineDto(
                x.Entry.Id.Value, x.Entry.PostingId.Value, x.Entry.BusinessDate, x.Entry.Description,
                x.ReferenceType, x.ReferenceId, x.Entry.Direction.ToString(), x.Entry.Amount, runningBalance));
        }

        FinanceReportMetadataDto metadata = query.FromDate is DateOnly from && query.ToDate is DateOnly to
            ? FinanceReportMetadataDto.ForPeriod(from, to, $"Flat {flat.FlatNumber}", clock.UtcNow, currentUser.UserId)
            : FinanceReportMetadataDto.ForAsOf(
                DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), $"Flat {flat.FlatNumber}", clock.UtcNow, currentUser.UserId);

        return new FlatFinancialStatementReportDto(
            metadata, flat.Id.Value, flat.FlatNumber, openingBalance, periodDebit, periodCredit, closingBalance,
            lines, page.Page, page.PageSize, page.Total);
    }

    // ResidentReceivable's normal balance is always Debit (LedgerAccountType.ResidentReceivable's doc
    // comment) — Debit entries add to the balance, Credit entries subtract, unconditionally.
    private static decimal SignedDelta(LedgerEntryWithReference x) =>
        x.Entry.Direction == LedgerDirection.Debit ? x.Entry.Amount : -x.Entry.Amount;
}
