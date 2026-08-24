using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.ExportFinancialSummaryReport;

public static class FinancialSummaryExportMapper
{
    public static ReportExportDocument ToExportDocument(FinancialSummaryDto report, string? buildingName)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Period", $"{FormatDate(report.FromDate)} to {FormatDate(report.ToDate)}"),
            ("As Of", FormatDate(report.AsOfDate)),
            ("Scope", buildingName ?? "All Buildings"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Metric"),
            new("Value", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            ["Total Billed", FormatAmount(report.TotalBilled)],
            ["Total Collected", FormatAmount(report.TotalCollected)],
            ["Total Outstanding", FormatAmount(report.TotalOutstanding)],
            ["Unpaid Invoices", report.UnpaidInvoiceCount.ToString(CultureInfo.InvariantCulture)],
            ["Partially Paid Invoices", report.PartiallyPaidInvoiceCount.ToString(CultureInfo.InvariantCulture)],
            ["Overdue Invoices", report.OverdueInvoiceCount.ToString(CultureInfo.InvariantCulture)],
        ];

        return new ReportExportDocument("Financial Summary", metadataLines, columns, rows);
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
