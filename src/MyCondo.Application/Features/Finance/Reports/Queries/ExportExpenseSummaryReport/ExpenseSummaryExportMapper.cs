using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseSummaryReport;

public static class ExpenseSummaryExportMapper
{
    public static ReportExportDocument ToExportDocument(ExpenseSummaryReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", report.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", report.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", report.Metadata.ScopeSummary),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Status"),
            new("Count", IsNumeric: true),
            new("Total Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.ByStatus
            .Select(line => (IReadOnlyList<string>)
            [
                line.Status,
                line.Count.ToString(CultureInfo.InvariantCulture),
                FormatAmount(line.TotalAmount),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Ledger Total", FormatAmount(report.LedgerTotal)),
            ("Source Record Total", FormatAmount(report.SourceRecordTotal)),
            ("Reconciled", report.IsReconciled ? "Yes" : "No"),
        ];

        return new ReportExportDocument("Expense Summary", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
