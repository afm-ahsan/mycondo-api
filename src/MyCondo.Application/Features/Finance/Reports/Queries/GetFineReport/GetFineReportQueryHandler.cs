using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;

/// <summary>Fine/penalty Billed/Collected/Waived for a period. Fines are not a separate domain feature
/// in this system — they are ordinary Invoices with <c>IncomeAccountType == FineIncome</c> (confirmed:
/// no <c>Fines</c> feature exists under <c>src/MyCondo.Domain/Features</c>; a fine is billed the same
/// way any other charge is, distinguished only by its income account type — ADR-027's Billing↔Finance
/// integration). See <c>IFinanceReportRepository.IncomeCollectionSummary</c>.</summary>
public sealed class GetFineReportQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFineReportQuery, FineReportDto>
{
    public async ValueTask<FineReportDto> Handle(GetFineReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IncomeCollectionSummary summary = await reports.GetIncomeCollectionAsync(
            tenantId, LedgerAccountType.FineIncome, query.FromDate, query.ToDate, cancellationToken);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (Fine invoices)", clock.UtcNow, currentUser.UserId);

        return new FineReportDto(
            metadata, summary.Billed, summary.BilledInvoiceCount, summary.Collected, summary.Waived);
    }
}
