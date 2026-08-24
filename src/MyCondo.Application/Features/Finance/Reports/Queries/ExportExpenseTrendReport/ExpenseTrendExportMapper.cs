using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseTrendReport;

public static class ExpenseTrendExportMapper
{
    public static ReportExportDocument ToExportDocument(ExpenseTrendReportDto report)
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
            new("Month"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Months
            .Select(month => (IReadOnlyList<string>)
            [
                $"{month.Year:D4}-{month.Month:D2}",
                FormatAmount(month.TotalAmount),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total", FormatAmount(report.Total)),
        ];

        return new ReportExportDocument("Expense Trend", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
