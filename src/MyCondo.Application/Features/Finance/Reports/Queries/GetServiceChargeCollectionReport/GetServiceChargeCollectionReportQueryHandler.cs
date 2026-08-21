using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

/// <summary>Service Charge Billed/Collected/Waived for a period — Invoices with
/// <c>IncomeAccountType == ServiceChargeIncome</c> are the join key from Billing back to the ledger
/// (ADR-027's Billing↔Finance integration). See <c>IFinanceReportRepository.IncomeCollectionSummary</c>.
/// </summary>
public sealed class GetServiceChargeCollectionReportQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetServiceChargeCollectionReportQuery, ServiceChargeCollectionReportDto>
{
    public async ValueTask<ServiceChargeCollectionReportDto> Handle(
        GetServiceChargeCollectionReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IncomeCollectionSummary summary = await reports.GetIncomeCollectionAsync(
            tenantId, LedgerAccountType.ServiceChargeIncome, query.FromDate, query.ToDate, cancellationToken);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (Service Charge invoices)", clock.UtcNow, currentUser.UserId);

        return new ServiceChargeCollectionReportDto(
            metadata, summary.Billed, summary.BilledInvoiceCount, summary.Collected, summary.Waived);
    }
}
