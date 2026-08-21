using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

/// <summary>Classifies Cash/Bank movements as Operating or Investing by the counter-leg's account
/// (see <c>IFinanceReportRepository.GetCashMovementsAsync</c>) — this system models no
/// loan/owner-capital accounts, so there is no Financing section to report.</summary>
public sealed class GetCashFlowQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetCashFlowQuery, CashFlowReportDto>
{
    public async ValueTask<CashFlowReportDto> Handle(GetCashFlowQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<Guid> cashAccountIds = await reports.GetCashAndBankChartOfAccountIdsAsync(tenantId, cancellationToken);

        decimal openingCashBalance = 0m;
        foreach (Guid accountId in cashAccountIds)
        {
            (decimal totalDebit, decimal totalCredit) = await reports.GetAccountBalanceBeforeAsync(
                tenantId, accountId, query.FromDate, cancellationToken);
            openingCashBalance += totalDebit - totalCredit;
        }

        IReadOnlyList<CashMovementLine> movements = await reports.GetCashMovementsAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        decimal operatingInflow = movements.Where(m => !m.IsInvestingCounterLeg && m.Direction == LedgerDirection.Debit).Sum(m => m.Amount);
        decimal operatingOutflow = movements.Where(m => !m.IsInvestingCounterLeg && m.Direction == LedgerDirection.Credit).Sum(m => m.Amount);
        decimal investingInflow = movements.Where(m => m.IsInvestingCounterLeg && m.Direction == LedgerDirection.Debit).Sum(m => m.Amount);
        decimal investingOutflow = movements.Where(m => m.IsInvestingCounterLeg && m.Direction == LedgerDirection.Credit).Sum(m => m.Amount);

        decimal netOperating = operatingInflow - operatingOutflow;
        decimal netInvesting = investingInflow - investingOutflow;
        decimal netChange = netOperating + netInvesting;

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all cash/bank accounts)", clock.UtcNow, currentUser.UserId);

        return new CashFlowReportDto(
            metadata, openingCashBalance, operatingInflow, operatingOutflow, netOperating,
            investingInflow, investingOutflow, netInvesting, netChange, openingCashBalance + netChange);
    }
}
