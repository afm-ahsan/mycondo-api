using System.Globalization;
using System.Text;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportAccountLedger;

/// <summary>Reuses <see cref="GetAccountLedgerQuery"/> for the actual report data (tenant scoping,
/// account lookup, running-balance derivation) rather than duplicating that logic. The on-screen browse
/// is paginated, but export must return the FULL matching result set — so this handler pages through
/// <see cref="GetAccountLedgerQuery"/> internally at <see cref="MaxPageSize"/> (the maximum
/// <c>GetAccountLedgerQueryValidator</c> allows per call) until every matching row has been retrieved.
/// Each call independently re-derives its own page's running balance from the preceding
/// <c>(page - 1) * pageSize</c> entries (see that handler), so concatenating pages fetched with a fixed
/// page size in order preserves a correct running balance throughout — no truncation, since the loop
/// keeps requesting pages until the accumulated row count reaches the server-reported
/// <see cref="AccountLedgerReportDto.Total"/> (or a page comes back empty).</summary>
public sealed class ExportAccountLedgerQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportAccountLedgerQuery, ReportExportResult>
{
    private const int MaxPageSize = 200;

    public async ValueTask<ReportExportResult> Handle(ExportAccountLedgerQuery query, CancellationToken cancellationToken)
    {
        List<AccountLedgerLineDto> allLines = [];
        AccountLedgerReportDto lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetAccountLedgerQuery(query.ChartOfAccountId, query.FromDate, query.ToDate, page, MaxPageSize),
                cancellationToken);
            allLines.AddRange(lastPage.Lines);
            page++;
        }
        while (lastPage.Lines.Count > 0 && allLines.Count < lastPage.Total);

        AccountLedgerReportDto fullReport = lastPage with { Lines = allLines, Page = 1, PageSize = allLines.Count };
        ReportExportDocument document = AccountLedgerExportMapper.ToExportDocument(fullReport);

        string fileName = $"account-ledger-{SanitizeForFileName(fullReport.ChartOfAccountCode)}-{BuildDateSegment(fullReport)}";
        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string BuildDateSegment(AccountLedgerReportDto report)
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
