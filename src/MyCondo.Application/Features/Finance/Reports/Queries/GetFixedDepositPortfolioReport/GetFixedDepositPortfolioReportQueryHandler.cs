using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

/// <summary>See <see cref="GetFixedDepositPortfolioReportQuery"/>. <c>TotalPrincipal</c> reuses
/// <see cref="IFixedDepositRepository.GetActivePrincipalTotalAsync"/> (the same figure the Financial
/// Overview report shows) rather than re-summing the loaded instrument list independently, so the two
/// reports can never disagree on "current Fixed Deposit principal."</summary>
public sealed class GetFixedDepositPortfolioReportQueryHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFixedDepositPortfolioReportQuery, FixedDepositPortfolioReportDto>
{
    public async ValueTask<FixedDepositPortfolioReportDto> Handle(
        GetFixedDepositPortfolioReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        List<FixedDeposit> deposits = await fixedDeposits.GetAllForTenantAsync(tenantId, cancellationToken);
        IReadOnlyList<FixedDepositAccrualTotal> accrualTotals = await accruals.GetTotalsByFixedDepositAsync(
            tenantId, fromDate: null, toDate: asOfDate, cancellationToken);
        IReadOnlyList<FixedDepositReceiptTotal> receiptTotals = await receipts.GetTotalsByFixedDepositAsync(
            tenantId, fromDate: null, toDate: asOfDate, cancellationToken);

        Dictionary<FixedDepositId, decimal> accruedById = accrualTotals.ToDictionary(a => a.FixedDepositId, a => a.TotalGrossAmount);
        Dictionary<FixedDepositId, decimal> receivedById = receiptTotals.ToDictionary(r => r.FixedDepositId, r => r.TotalGrossAmount);

        List<FixedDepositPortfolioLineDto> lines = deposits
            .Select(fd =>
            {
                decimal accrued = accruedById.GetValueOrDefault(fd.Id);
                decimal received = receivedById.GetValueOrDefault(fd.Id);
                return new FixedDepositPortfolioLineDto(
                    fd.Id.Value, fd.CertificateNumber, fd.BankName, fd.BranchName, fd.Principal, fd.InterestRatePercent,
                    fd.StartDate, fd.MaturityDate, fd.Status.ToString(), accrued, received, accrued - received);
            })
            .ToList();

        decimal totalPrincipal = await fixedDeposits.GetActivePrincipalTotalAsync(tenantId, cancellationToken);
        decimal totalOutstanding = lines.Sum(l => l.OutstandingAccruedInterest);
        int activeCount = deposits.Count(fd => fd.Status == FixedDepositStatus.Active);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all Fixed Deposits)", clock.UtcNow, currentUser.UserId);

        return new FixedDepositPortfolioReportDto(metadata, lines, totalPrincipal, totalOutstanding, activeCount);
    }
}
