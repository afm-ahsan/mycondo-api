using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportCashFlow;

public static class CashFlowExportMapper
{
    public static ReportExportDocument ToExportDocument(CashFlowReportDto report)
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
            new("Metric"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            ["Opening Cash Balance", FormatAmount(report.OpeningCashBalance)],
            ["Operating Inflow", FormatAmount(report.OperatingInflow)],
            ["Operating Outflow", FormatAmount(report.OperatingOutflow)],
            ["Net Operating", FormatAmount(report.NetOperating)],
            ["Investing Inflow", FormatAmount(report.InvestingInflow)],
            ["Investing Outflow", FormatAmount(report.InvestingOutflow)],
            ["Net Investing", FormatAmount(report.NetInvesting)],
            ["Net Change in Cash", FormatAmount(report.NetChangeInCash)],
            ["Closing Cash Balance", FormatAmount(report.ClosingCashBalance)],
        ];

        return new ReportExportDocument("Cash Flow", metadataLines, columns, rows);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
