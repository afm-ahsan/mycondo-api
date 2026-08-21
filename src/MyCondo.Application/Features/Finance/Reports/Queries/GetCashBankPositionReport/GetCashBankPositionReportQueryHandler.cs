using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;

/// <summary>Per-<see cref="FinancialAccount"/> ledger balance as of a date, grouped/labeled by
/// <see cref="FinancialAccountType"/> (Cash/Bank/MobileFinancialService — Template 4). Reuses
/// <c>GetTrialBalanceAsync</c>'s cumulative-to-date per-account totals rather than a second balance
/// query per account, since each FinancialAccount already owns its own child ChartOfAccountId (Template
/// 4) and the Trial Balance necessarily includes every account with posted activity.</summary>
public sealed class GetCashBankPositionReportQueryHandler(
    IFinanceReportRepository reports,
    IFinancialAccountRepository financialAccounts,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetCashBankPositionReportQuery, CashBankPositionReportDto>
{
    public async ValueTask<CashBankPositionReportDto> Handle(GetCashBankPositionReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        List<FinancialAccount> accounts = await financialAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
        IReadOnlyList<TrialBalanceAccountLine> balances = await reports.GetTrialBalanceAsync(tenantId, asOfDate, cancellationToken);

        Dictionary<ChartOfAccountId, TrialBalanceAccountLine> balancesByAccount =
            balances.ToDictionary(b => b.ChartOfAccountId);

        List<CashBankAccountLineDto> lines = accounts
            .Select(a =>
            {
                decimal balance = balancesByAccount.TryGetValue(a.ChartOfAccountId, out TrialBalanceAccountLine? line)
                    ? line.TotalDebit - line.TotalCredit
                    : 0m;

                return new CashBankAccountLineDto(
                    a.Id.Value, a.Name, a.AccountType.ToString(), a.BankName, a.BranchName, a.AccountNumber, balance);
            })
            .OrderBy(l => l.AccountType)
            .ThenBy(l => l.Name)
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all financial accounts)", clock.UtcNow, currentUser.UserId);

        return new CashBankPositionReportDto(metadata, lines, lines.Sum(l => l.Balance));
    }
}
