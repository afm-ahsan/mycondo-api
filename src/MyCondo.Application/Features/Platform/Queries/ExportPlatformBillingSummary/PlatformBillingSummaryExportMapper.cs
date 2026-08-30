using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;

public static class PlatformBillingSummaryExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<PlatformBillingSummaryCurrencyDto> summary, DateOnly? dateFrom, DateOnly? dateTo)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", "CondoBD Platform Subscription Billing / Collections — not a GAAP/IFRS financial statement"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Currency"),
            new("Invoiced Amount", IsNumeric: true),
            new("Invoiced Invoices", IsNumeric: true),
            new("Collected Amount", IsNumeric: true),
            new("Collected Payments", IsNumeric: true),
            new("Outstanding Amount", IsNumeric: true),
            new("Outstanding Invoices", IsNumeric: true),
            new("Overdue Outstanding Amount", IsNumeric: true),
            new("Overdue Invoices", IsNumeric: true),
            new("Max Days Overdue", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = summary
            .Select(x => (IReadOnlyList<string>)
            [
                x.Currency,
                FormatAmount(x.InvoicedAmount),
                x.InvoicedInvoiceCount.ToString(CultureInfo.InvariantCulture),
                FormatAmount(x.CollectedAmount),
                x.CollectedPaymentCount.ToString(CultureInfo.InvariantCulture),
                FormatAmount(x.OutstandingAmount),
                x.OutstandingInvoiceCount.ToString(CultureInfo.InvariantCulture),
                FormatAmount(x.OverdueOutstandingAmount),
                x.OverdueInvoiceCount.ToString(CultureInfo.InvariantCulture),
                x.MaxDaysOverdue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ])
            .ToList();

        return new ReportExportDocument("Platform Subscription Billing Summary", metadataLines, columns, rows);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
