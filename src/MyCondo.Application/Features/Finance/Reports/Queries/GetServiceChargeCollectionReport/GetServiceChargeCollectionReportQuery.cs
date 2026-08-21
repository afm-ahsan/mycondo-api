using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

/// <summary>Billed/Collected/Waived stay permanently distinct fields — see
/// <c>IFinanceReportRepository.IncomeCollectionSummary</c>'s doc comment. Never merge Billed and
/// Collected into a single "revenue" figure. Deliberately carries no derived "Outstanding" figure:
/// Billed and Collected are independent period-flow windows (Collected can settle an invoice billed in
/// an earlier period), so Billed - Collected here would not equal actual outstanding balance — that
/// figure belongs to the Receivable Ageing / Outstanding Dues reports, which compute it correctly from
/// each invoice's own <c>Balance</c>.</summary>
public sealed record ServiceChargeCollectionReportDto(
    FinanceReportMetadataDto Metadata,
    decimal Billed,
    int BilledInvoiceCount,
    decimal Collected,
    decimal Waived);

public sealed record GetServiceChargeCollectionReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ServiceChargeCollectionReportDto>;
