using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;

/// <summary>Billed/Collected/Waived stay permanently distinct fields — see
/// <c>IFinanceReportRepository.IncomeCollectionSummary</c>'s doc comment. <see cref="Waived"/> is
/// particularly relevant here: fines are the primary real-world use of <c>Invoice.Waive</c> (see that
/// method's "e.g. a fine waiver" doc comment). Deliberately carries no derived "Outstanding" figure —
/// see <c>ServiceChargeCollectionReportDto</c>'s doc comment for why.</summary>
public sealed record FineReportDto(
    FinanceReportMetadataDto Metadata,
    decimal Billed,
    int BilledInvoiceCount,
    decimal Collected,
    decimal Waived);

public sealed record GetFineReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<FineReportDto>;
