using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.ExportResidentLedger;

public static class ResidentLedgerExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<LedgerEntryDto> entries, DateOnly? fromDate, DateOnly? toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
        ];

        List<ReportExportColumn> columns =
        [
            new("Date"),
            new("Description"),
            new("Reference Type"),
            new("Direction"),
            new("Amount", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = entries
            .Select(entry => (IReadOnlyList<string>)
            [
                entry.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                entry.Description,
                entry.ReferenceType ?? string.Empty,
                entry.Direction,
                FormatAmount(entry.Amount),
            ])
            .ToList();

        decimal totalDebit = entries.Where(e => e.Direction == "Debit").Sum(e => e.Amount);
        decimal totalCredit = entries.Where(e => e.Direction == "Credit").Sum(e => e.Amount);

        List<(string Label, string Value)> totals =
        [
            ("Total Debit", FormatAmount(totalDebit)),
            ("Total Credit", FormatAmount(totalCredit)),
        ];

        return new ReportExportDocument("Resident Ledger", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
