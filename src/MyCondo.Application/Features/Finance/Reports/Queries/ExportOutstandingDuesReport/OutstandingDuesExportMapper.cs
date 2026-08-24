using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;

public static class OutstandingDuesExportMapper
{
    public static ReportExportDocument ToExportDocument(OutstandingDuesReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("As Of", report.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", report.Metadata.ScopeSummary),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Flat"),
            new("Outstanding Balance", IsNumeric: true),
            new("Open Invoices", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.FlatNumber,
                FormatAmount(line.OutstandingBalance),
                line.OpenInvoiceCount.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Outstanding", FormatAmount(report.TotalOutstanding)),
        ];

        return new ReportExportDocument("Outstanding Dues", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
