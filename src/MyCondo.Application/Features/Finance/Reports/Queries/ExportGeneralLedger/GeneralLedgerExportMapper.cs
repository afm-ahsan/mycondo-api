using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportGeneralLedger;

public static class GeneralLedgerExportMapper
{
    public static ReportExportDocument ToExportDocument(GeneralLedgerReportDto report)
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
            new("Date"),
            new("Description"),
            new("Reference Type"),
            new("Account Code"),
            new("Account"),
            new("Direction"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                line.Description,
                line.ReferenceType ?? string.Empty,
                line.ChartOfAccountCode ?? string.Empty,
                line.ChartOfAccountName ?? string.Empty,
                line.Direction,
                FormatAmount(line.Amount),
            ])
            .ToList();

        decimal totalDebit = report.Lines.Where(l => l.Direction == "Debit").Sum(l => l.Amount);
        decimal totalCredit = report.Lines.Where(l => l.Direction == "Credit").Sum(l => l.Amount);

        List<(string Label, string Value)> totals =
        [
            ("Total Debit", FormatAmount(totalDebit)),
            ("Total Credit", FormatAmount(totalCredit)),
        ];

        return new ReportExportDocument("General Ledger", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
