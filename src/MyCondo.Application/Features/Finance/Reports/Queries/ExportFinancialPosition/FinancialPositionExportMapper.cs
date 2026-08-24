using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialPosition;

public static class FinancialPositionExportMapper
{
    public static ReportExportDocument ToExportDocument(FinancialPositionReportDto report)
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
            new("Section"),
            new("Code"),
            new("Account"),
            new("Balance", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            .. report.Assets.Lines.Select(l => SectionRow("Assets", l)),
            .. report.Liabilities.Lines.Select(l => SectionRow("Liabilities", l)),
            .. report.Equity.Lines.Select(l => SectionRow("Equity", l)),
        ];

        List<(string Label, string Value)> totals =
        [
            ("Total Assets", FormatAmount(report.Assets.Total)),
            ("Total Liabilities", FormatAmount(report.Liabilities.Total)),
            ("Total Equity", FormatAmount(report.Equity.Total)),
            ("Retained Surplus/Deficit", FormatAmount(report.RetainedSurplusDeficit)),
            ("Total Liabilities & Equity", FormatAmount(report.TotalLiabilitiesAndEquity)),
        ];

        return new ReportExportDocument("Financial Position", metadataLines, columns, rows, totals);
    }

    private static IReadOnlyList<string> SectionRow(string section, FinancialPositionLineDto line) =>
        [section, line.Code, line.Name, FormatAmount(line.Balance)];

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
