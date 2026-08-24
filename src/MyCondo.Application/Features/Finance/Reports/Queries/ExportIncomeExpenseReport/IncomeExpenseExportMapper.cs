using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;

public static class IncomeExpenseExportMapper
{
    public static ReportExportDocument ToExportDocument(IncomeExpenseReportDto report)
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
            new("Section"),
            new("Code"),
            new("Account"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            .. report.IncomeLines.Select(l => SectionRow("Income", l)),
            .. report.ExpenseLines.Select(l => SectionRow("Expense", l)),
        ];

        List<(string Label, string Value)> totals =
        [
            ("Total Income", FormatAmount(report.TotalIncome)),
            ("Total Expense", FormatAmount(report.TotalExpense)),
            ("Surplus/Deficit", FormatAmount(report.SurplusDeficit)),
        ];

        return new ReportExportDocument("Income & Expense", metadataLines, columns, rows, totals);
    }

    private static IReadOnlyList<string> SectionRow(string section, IncomeExpenseLineDto line) =>
        [section, line.Code, line.Name, FormatAmount(line.Amount)];

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
