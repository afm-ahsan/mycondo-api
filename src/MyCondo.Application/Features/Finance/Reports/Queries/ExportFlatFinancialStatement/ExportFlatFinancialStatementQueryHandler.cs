using System.Globalization;
using System.Text;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFlatFinancialStatement;

/// <summary>Reuses <see cref="GetFlatFinancialStatementQuery"/> for the actual report data (tenant
/// scoping, opening/closing balance, running-balance derivation) rather than duplicating that logic.
/// The on-screen browse is paginated, but export must return the FULL matching result set — so this
/// handler pages through <see cref="GetFlatFinancialStatementQuery"/> internally at
/// <see cref="MaxPageSize"/> (the maximum <c>GetFlatFinancialStatementQueryValidator</c> allows per
/// call — 100, lower than the other ledger/statement reports' 200) until every matching row has been
/// retrieved. Each call independently re-derives its own page's running balance from the preceding
/// entries, so concatenating pages fetched with a fixed page size in order preserves a correct running
/// balance throughout — no truncation, since the loop keeps requesting pages until the accumulated row
/// count reaches the server-reported <see cref="FlatFinancialStatementReportDto.Total"/> (or a page
/// comes back empty).</summary>
public sealed class ExportFlatFinancialStatementQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFlatFinancialStatementQuery, ReportExportResult>
{
    private const int MaxPageSize = 100;

    public async ValueTask<ReportExportResult> Handle(ExportFlatFinancialStatementQuery query, CancellationToken cancellationToken)
    {
        List<FlatFinancialStatementLineDto> allLines = [];
        FlatFinancialStatementReportDto lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetFlatFinancialStatementQuery(query.FlatId, query.FromDate, query.ToDate, page, MaxPageSize),
                cancellationToken);
            allLines.AddRange(lastPage.Lines);
            page++;
        }
        while (lastPage.Lines.Count > 0 && allLines.Count < lastPage.Total);

        FlatFinancialStatementReportDto fullReport = lastPage with { Lines = allLines, Page = 1, PageSize = allLines.Count };
        ReportExportDocument document = FlatFinancialStatementExportMapper.ToExportDocument(fullReport);

        string fileName = $"flat-statement-{SanitizeForFileName(fullReport.FlatNumber)}-{BuildDateSegment(fullReport)}";
        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string BuildDateSegment(FlatFinancialStatementReportDto report)
    {
        if (report.Metadata.FromDate is DateOnly from && report.Metadata.ToDate is DateOnly to)
        {
            return $"{from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-to-{to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        }

        DateOnly asOfDate = report.Metadata.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string SanitizeForFileName(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        return builder.ToString();
    }
}
