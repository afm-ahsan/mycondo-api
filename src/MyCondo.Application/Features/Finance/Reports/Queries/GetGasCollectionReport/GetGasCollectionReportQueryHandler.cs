using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;

/// <summary>Gas recovery Billed/Collected/Waived for a period — Invoices with
/// <c>IncomeAccountType == GasRecoveryIncome</c> (Utility invoices where <c>UtilityType == Gas</c>) are
/// the join key back to the ledger (ADR-027's Billing↔Finance integration). See
/// <c>IFinanceReportRepository.IncomeCollectionSummary</c>.</summary>
public sealed class GetGasCollectionReportQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetGasCollectionReportQuery, GasCollectionReportDto>
{
    public async ValueTask<GasCollectionReportDto> Handle(GetGasCollectionReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IncomeCollectionSummary summary = await reports.GetIncomeCollectionAsync(
            tenantId, LedgerAccountType.GasRecoveryIncome, query.FromDate, query.ToDate, cancellationToken);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (Gas recovery invoices)", clock.UtcNow, currentUser.UserId);

        return new GasCollectionReportDto(
            metadata, summary.Billed, summary.BilledInvoiceCount, summary.Collected, summary.Waived);
    }
}
