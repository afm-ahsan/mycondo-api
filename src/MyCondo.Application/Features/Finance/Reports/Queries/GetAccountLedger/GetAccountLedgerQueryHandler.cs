using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;

/// <summary>Single-account ledger with a running balance. The opening balance is derived from
/// entries strictly before <see cref="GetAccountLedgerQuery.FromDate"/> (0 when no FromDate is
/// given — the report then covers the account's full history). Running balance for pages beyond the
/// first re-sums the preceding page(s) via a bounded offset query rather than a stored/cached
/// balance, honoring "closing balance is always derived from ledger entries, never stored" — deep
/// pagination on a very high-volume account is the known cost of that guarantee.</summary>
public sealed class GetAccountLedgerQueryHandler(
    IFinanceReportRepository reports,
    IChartOfAccountRepository chartOfAccounts,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetAccountLedgerQuery, AccountLedgerReportDto>
{
    public async ValueTask<AccountLedgerReportDto> Handle(GetAccountLedgerQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ChartOfAccountId accountId = new(query.ChartOfAccountId);
        ChartOfAccount? account = await chartOfAccounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null || account.TenantId != tenantId)
        {
            throw new NotFoundException("ChartOfAccount", query.ChartOfAccountId);
        }

        bool isDebitNormal = account.NormalBalance == LedgerDirection.Debit;

        decimal openingBalance = 0m;
        if (query.FromDate is DateOnly fromDate)
        {
            (decimal totalDebit, decimal totalCredit) = await reports.GetAccountBalanceBeforeAsync(
                tenantId, query.ChartOfAccountId, fromDate, cancellationToken);
            openingBalance = isDebitNormal ? totalDebit - totalCredit : totalCredit - totalDebit;
        }

        decimal precedingBalance = 0m;
        if (query.Page > 1)
        {
            PagedResult<LedgerActivityLine> preceding = await reports.GetGeneralLedgerAsync(
                tenantId, query.FromDate, query.ToDate, query.ChartOfAccountId, fundId: null, referenceType: null,
                page: 1, pageSize: (query.Page - 1) * query.PageSize, ascending: true, cancellationToken);

            precedingBalance = preceding.Items.Sum(l => SignedDelta(l, isDebitNormal));
        }

        PagedResult<LedgerActivityLine> page = await reports.GetGeneralLedgerAsync(
            tenantId, query.FromDate, query.ToDate, query.ChartOfAccountId, fundId: null, referenceType: null,
            query.Page, query.PageSize, ascending: true, cancellationToken);

        decimal runningBalance = openingBalance + precedingBalance;
        List<AccountLedgerLineDto> lines = [];
        foreach (LedgerActivityLine l in page.Items)
        {
            runningBalance += SignedDelta(l, isDebitNormal);
            lines.Add(new AccountLedgerLineDto(
                l.EntryId.Value, l.PostingId.Value, l.BusinessDate, l.Description, l.ReferenceType, l.ReferenceId,
                l.FlatId?.Value, l.Direction.ToString(), l.Amount, runningBalance));
        }

        FinanceReportMetadataDto metadata = query.FromDate is DateOnly from && query.ToDate is DateOnly to
            ? FinanceReportMetadataDto.ForPeriod(from, to, $"Account {account.Code}", clock.UtcNow, currentUser.UserId)
            : FinanceReportMetadataDto.ForAsOf(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), $"Account {account.Code}", clock.UtcNow, currentUser.UserId);

        return new AccountLedgerReportDto(
            metadata, account.Id.Value, account.Code, account.Name, account.NormalBalance.ToString(),
            openingBalance, lines, runningBalance, page.Page, page.PageSize, page.Total);
    }

    private static decimal SignedDelta(LedgerActivityLine line, bool isDebitNormal)
    {
        bool isDebit = line.Direction == LedgerDirection.Debit;
        bool addsToBalance = isDebit == isDebitNormal;
        return addsToBalance ? line.Amount : -line.Amount;
    }
}
