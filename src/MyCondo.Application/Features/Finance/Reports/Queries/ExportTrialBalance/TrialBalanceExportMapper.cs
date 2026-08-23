using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportTrialBalance;

public static class TrialBalanceExportMapper
{
    public static ReportExportDocument ToExportDocument(TrialBalanceReportDto report)
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
            new("Code"),
            new("Account"),
            new("Category"),
            new("Debit", IsNumeric: true),
            new("Credit", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.Code,
                line.Name,
                line.Category,
                FormatAmount(line.Debit),
                FormatAmount(line.Credit),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Debit", FormatAmount(report.TotalDebit)),
            ("Total Credit", FormatAmount(report.TotalCredit)),
        ];

        return new ReportExportDocument("Trial Balance", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
