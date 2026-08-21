using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

public sealed record OutstandingDuesLineDto(
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    decimal OutstandingBalance,
    int OpenInvoiceCount);

public sealed record OutstandingDuesReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<OutstandingDuesLineDto> Lines,
    decimal TotalOutstanding);

/// <summary>Simple per-flat/per-resident total outstanding balance — not bucketed by age (that's the
/// existing Receivable Ageing report). <paramref name="BuildingId"/> narrows to one building; omit for
/// the tenant-wide list.</summary>
public sealed record GetOutstandingDuesReportQuery(Guid? BuildingId) : IRequest<OutstandingDuesReportDto>;
