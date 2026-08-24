using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportGeneralLedger;

/// <summary>Reuses <see cref="GetGeneralLedgerQuery"/> for the actual report data (tenant scoping,
/// filtering) rather than duplicating that logic. The on-screen browse is paginated, but export must
/// return the FULL matching result set — so this handler pages through
/// <see cref="GetGeneralLedgerQuery"/> internally at <see cref="MaxPageSize"/> (the maximum
/// <c>GetGeneralLedgerQueryValidator</c> allows per call) until every matching row has been retrieved,
/// then maps the concatenated lines to one <see cref="ReportExportDocument"/>. This never truncates:
/// the loop keeps requesting pages until the accumulated row count reaches the server-reported
/// <see cref="GeneralLedgerReportDto.Total"/> (or a page comes back empty), so an arbitrarily large
/// result set is fully exported at the cost of additional round-trips rather than a capped export.
/// </summary>
public sealed class ExportGeneralLedgerQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportGeneralLedgerQuery, ReportExportResult>
{
    private const int MaxPageSize = 200;

    public async ValueTask<ReportExportResult> Handle(ExportGeneralLedgerQuery query, CancellationToken cancellationToken)
    {
        List<GeneralLedgerLineDto> allLines = [];
        GeneralLedgerReportDto lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetGeneralLedgerQuery(
                    query.FromDate, query.ToDate, query.ChartOfAccountId, query.FundId, query.ReferenceType,
                    page, MaxPageSize),
                cancellationToken);
            allLines.AddRange(lastPage.Lines);
            page++;
        }
        while (lastPage.Lines.Count > 0 && allLines.Count < lastPage.Total);

        GeneralLedgerReportDto fullReport = lastPage with { Lines = allLines, Page = 1, PageSize = allLines.Count };
        ReportExportDocument document = GeneralLedgerExportMapper.ToExportDocument(fullReport);

        string fileName = $"general-ledger-{BuildDateSegment(fullReport)}";
        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string BuildDateSegment(GeneralLedgerReportDto report)
    {
        if (report.Metadata.FromDate is DateOnly from && report.Metadata.ToDate is DateOnly to)
        {
            return $"{from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-to-{to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        }

        DateOnly asOfDate = report.Metadata.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
