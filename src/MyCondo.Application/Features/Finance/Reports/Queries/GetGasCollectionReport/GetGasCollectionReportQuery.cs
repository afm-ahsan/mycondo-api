using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;

/// <summary>Billed/Collected/Waived stay permanently distinct fields — see
/// <c>IFinanceReportRepository.IncomeCollectionSummary</c>'s doc comment. Deliberately carries no
/// derived "Outstanding" figure — see <c>ServiceChargeCollectionReportDto</c>'s doc comment for why.
/// </summary>
public sealed record GasCollectionReportDto(
    FinanceReportMetadataDto Metadata,
    decimal Billed,
    int BilledInvoiceCount,
    decimal Collected,
    decimal Waived);

public sealed record GetGasCollectionReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<GasCollectionReportDto>;
