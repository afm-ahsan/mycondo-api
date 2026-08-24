using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payments.Queries.ExportResidentLedger;

/// <summary>Reuses <see cref="GetLedgerEntriesForAccountQuery"/> for the actual report data (tenant
/// scoping, flat lookup, filtering) rather than duplicating that logic. The on-screen browse is
/// paginated, but export must return the FULL matching result set — so this handler pages through
/// <see cref="GetLedgerEntriesForAccountQuery"/> internally at <see cref="MaxPageSize"/> (the maximum
/// <c>GetLedgerEntriesForAccountQueryValidator</c> allows per call) until every matching row has been
/// retrieved. <see cref="LedgerEntryDto"/> carries no running balance (unlike the chart-of-account
/// ledger reports), so there is nothing that pagination-order could invalidate — this never truncates:
/// the loop keeps requesting pages until the accumulated row count reaches the server-reported
/// <see cref="PagedResult{T}.Total"/> (or a page comes back empty).</summary>
public sealed class ExportResidentLedgerQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportResidentLedgerQuery, ReportExportResult>
{
    private const int MaxPageSize = 100;

    public async ValueTask<ReportExportResult> Handle(ExportResidentLedgerQuery query, CancellationToken cancellationToken)
    {
        List<LedgerEntryDto> allEntries = [];
        PagedResult<LedgerEntryDto> lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetLedgerEntriesForAccountQuery(
                    query.FlatId, query.FromDate, query.ToDate, query.ReferenceType, page, MaxPageSize),
                cancellationToken);
            allEntries.AddRange(lastPage.Items);
            page++;
        }
        while (lastPage.Items.Count > 0 && allEntries.Count < lastPage.Total);

        ReportExportDocument document = ResidentLedgerExportMapper.ToExportDocument(allEntries, query.FromDate, query.ToDate);

        string fileName = $"resident-ledger-{query.FlatId.ToString("N", CultureInfo.InvariantCulture)[..8]}-{BuildDateSegment(query)}";
        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string BuildDateSegment(ExportResidentLedgerQuery query)
    {
        if (query.FromDate is DateOnly from && query.ToDate is DateOnly to)
        {
            return $"{from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-to-{to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        }

        return DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
