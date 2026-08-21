using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

/// <summary>See <see cref="FixedDepositInterestReportDto"/> for the verified accrual-vs-receipt posting
/// split this handler's reconciliation identity relies on.</summary>
public sealed class GetFixedDepositInterestReportQueryHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFixedDepositInterestReportQuery, FixedDepositInterestReportDto>
{
    public async ValueTask<FixedDepositInterestReportDto> Handle(
        GetFixedDepositInterestReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<FixedDepositAccrualTotal> accrualPeriod = await accruals.GetTotalsByFixedDepositAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);
        IReadOnlyList<FixedDepositReceiptTotal> receiptPeriod = await receipts.GetTotalsByFixedDepositAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        decimal accruedGrossForPeriod = accrualPeriod.Sum(a => a.TotalGrossAmount);
        decimal receivedGrossForPeriod = receiptPeriod.Sum(r => r.TotalGrossAmount);
        decimal receivedDeductionForPeriod = receiptPeriod.Sum(r => r.TotalDeductionAmount);
        decimal receivedNetForPeriod = receivedGrossForPeriod - receivedDeductionForPeriod;

        decimal ledgerInterestIncomeForPeriod = 0m;
        Guid? fdInterestIncomeAccountId = await reports.GetChartOfAccountIdForPostingRoleAsync(
            tenantId, nameof(LedgerAccountType.FDInterestIncome), cancellationToken);
        if (fdInterestIncomeAccountId is Guid accountId)
        {
            IReadOnlyList<TrialBalanceAccountLine> incomeActivity = await reports.GetCategoryActivityAsync(
                tenantId, AccountCategory.Income, query.FromDate, query.ToDate, cancellationToken);
            TrialBalanceAccountLine? fdInterestLine = incomeActivity.FirstOrDefault(l => l.ChartOfAccountId.Value == accountId);
            if (fdInterestLine is not null)
            {
                ledgerInterestIncomeForPeriod = fdInterestLine.TotalCredit - fdInterestLine.TotalDebit;
            }
        }

        // Cumulative-to-ToDate outstanding balance — mirrors the ledger's InterestReceivable account.
        IReadOnlyList<FixedDepositAccrualTotal> accrualCumulative = await accruals.GetTotalsByFixedDepositAsync(
            tenantId, fromDate: null, query.ToDate, cancellationToken);
        IReadOnlyList<FixedDepositReceiptTotal> receiptCumulative = await receipts.GetTotalsByFixedDepositAsync(
            tenantId, fromDate: null, query.ToDate, cancellationToken);
        decimal outstanding = accrualCumulative.Sum(a => a.TotalGrossAmount) - receiptCumulative.Sum(r => r.TotalGrossAmount);

        List<FixedDeposit> deposits = await fixedDeposits.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<FixedDepositId, string> certificateById = deposits.ToDictionary(fd => fd.Id, fd => fd.CertificateNumber);

        Dictionary<FixedDepositId, FixedDepositAccrualTotal> accrualById = accrualPeriod.ToDictionary(a => a.FixedDepositId);
        Dictionary<FixedDepositId, FixedDepositReceiptTotal> receiptById = receiptPeriod.ToDictionary(r => r.FixedDepositId);

        List<FixedDepositInterestLineDto> byFixedDeposit = accrualById.Keys
            .Union(receiptById.Keys)
            .Select(id =>
            {
                decimal accGross = accrualById.TryGetValue(id, out FixedDepositAccrualTotal? a) ? a.TotalGrossAmount : 0m;
                decimal recGross = receiptById.TryGetValue(id, out FixedDepositReceiptTotal? r) ? r.TotalGrossAmount : 0m;
                decimal recDeduction = receiptById.TryGetValue(id, out FixedDepositReceiptTotal? r2) ? r2.TotalDeductionAmount : 0m;
                string certificateNumber = certificateById.GetValueOrDefault(id, "Unknown");
                return new FixedDepositInterestLineDto(id.Value, certificateNumber, accGross, recGross, recDeduction, recGross - recDeduction);
            })
            .OrderBy(l => l.CertificateNumber)
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all Fixed Deposits)", clock.UtcNow, currentUser.UserId);

        return new FixedDepositInterestReportDto(
            metadata, accruedGrossForPeriod, ledgerInterestIncomeForPeriod, accruedGrossForPeriod == ledgerInterestIncomeForPeriod,
            receivedGrossForPeriod, receivedDeductionForPeriod, receivedNetForPeriod, outstanding, byFixedDeposit);
    }
}
