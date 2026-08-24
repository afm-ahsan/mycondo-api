using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportCashBankPositionReport;

public static class CashBankPositionExportMapper
{
    public static ReportExportDocument ToExportDocument(CashBankPositionReportDto report)
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
            new("Account"),
            new("Type"),
            new("Bank"),
            new("Branch"),
            new("Account Number"),
            new("Balance", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Accounts
            .Select(a => (IReadOnlyList<string>)
            [
                a.Name,
                a.AccountType,
                a.BankName ?? string.Empty,
                a.BranchName ?? string.Empty,
                a.AccountNumber ?? string.Empty,
                FormatAmount(a.Balance),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Balance", FormatAmount(report.TotalBalance)),
        ];

        return new ReportExportDocument("Cash & Bank Position", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
