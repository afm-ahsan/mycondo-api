using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.ExportReceivablesAgeingReport;

public static class ReceivablesAgeingExportMapper
{
    public static ReportExportDocument ToExportDocument(ReceivablesAgeingReportDto report, string? buildingName)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("As Of", report.AsOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Scope", buildingName ?? "All Buildings"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Ageing Bucket"),
            new("Invoice Count", IsNumeric: true),
            new("Balance", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Buckets
            .Select(bucket => (IReadOnlyList<string>)
            [
                bucket.BucketLabel,
                bucket.InvoiceCount.ToString(CultureInfo.InvariantCulture),
                FormatAmount(bucket.TotalBalance),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Grand Total", FormatAmount(report.GrandTotal)),
        ];

        return new ReportExportDocument("Receivables Ageing", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
