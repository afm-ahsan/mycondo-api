using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportAccountLedger;

public static class AccountLedgerExportMapper
{
    public static ReportExportDocument ToExportDocument(AccountLedgerReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Account", $"{report.ChartOfAccountCode} - {report.ChartOfAccountName}"),
            ("Normal Balance", report.NormalBalance),
            ("From", report.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", report.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Date"),
            new("Description"),
            new("Reference Type"),
            new("Direction"),
            new("Amount", IsNumeric: true),
            new("Running Balance", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                line.Description,
                line.ReferenceType ?? string.Empty,
                line.Direction,
                FormatAmount(line.Amount),
                FormatAmount(line.RunningBalance),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Opening Balance", FormatAmount(report.OpeningBalance)),
            ("Closing Balance", FormatAmount(report.ClosingBalance)),
        ];

        return new ReportExportDocument("Account Ledger", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
