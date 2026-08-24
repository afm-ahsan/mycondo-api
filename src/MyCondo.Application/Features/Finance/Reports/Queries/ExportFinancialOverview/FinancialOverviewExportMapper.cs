using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialOverview;

public static class FinancialOverviewExportMapper
{
    public static ReportExportDocument ToExportDocument(FinancialOverviewReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("As Of", report.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Collection Period From", report.CollectionPeriodFromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Collection Period To", report.CollectionPeriodToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Scope", report.Metadata.ScopeSummary),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Expense Category"),
            new("Amount", IsNumeric: true),
            new("% of Total", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.ExpenseComposition
            .Select(line => (IReadOnlyList<string>)
            [
                line.CategoryName,
                FormatAmount(line.Amount),
                line.PercentageOfTotal.ToString("N2", CultureInfo.InvariantCulture),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Income Total", FormatAmount(report.IncomeTotal)),
            ("Expense Total", FormatAmount(report.ExpenseTotal)),
            ("Surplus/Deficit", FormatAmount(report.SurplusDeficit)),
            ("Receivables", FormatAmount(report.Receivables)),
            ("Cash In Hand", FormatAmount(report.CashPosition.CashInHand)),
            ("Bank Balance", FormatAmount(report.CashPosition.BankBalance)),
            ("Mobile Financial Service Balance", FormatAmount(report.CashPosition.MobileFinancialServiceBalance)),
            ("Available Liquid Funds", FormatAmount(report.CashPosition.AvailableLiquidFunds)),
            ("Fixed Deposit Principal", FormatAmount(report.FixedDepositPrincipal)),
            ("Collection Period Billed", FormatAmount(report.CollectionPerformance.Billed)),
            ("Collection Period Collected", FormatAmount(report.CollectionPerformance.Collected)),
        ];

        return new ReportExportDocument("Financial Overview", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
