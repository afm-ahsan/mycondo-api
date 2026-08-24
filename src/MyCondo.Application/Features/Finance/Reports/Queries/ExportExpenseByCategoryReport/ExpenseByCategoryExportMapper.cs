using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;

public static class ExpenseByCategoryExportMapper
{
    public static ReportExportDocument ToExportDocument(ExpenseByCategoryReportDto report)
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
            new("Category"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.CategoryName,
                FormatAmount(line.TotalAmount),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total", FormatAmount(report.Total)),
        ];

        return new ReportExportDocument("Expense by Category", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
