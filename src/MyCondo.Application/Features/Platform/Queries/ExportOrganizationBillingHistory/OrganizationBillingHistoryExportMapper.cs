using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;

public static class OrganizationBillingHistoryExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<OrganizationBillingHistoryEventDto> events, DateOnly? dateFrom, DateOnly? dateTo)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", "CondoBD Platform Subscription Billing / Collections — not a GAAP/IFRS financial statement"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Date"),
            new("Event"),
            new("Invoice Number"),
            new("Status"),
            new("Currency"),
            new("Amount", IsNumeric: true),
            new("Outstanding Amount", IsNumeric: true),
            new("Days Overdue", IsNumeric: true),
            new("Reference Number"),
        ];

        List<IReadOnlyList<string>> rows = events
            .Select(x => (IReadOnlyList<string>)
            [
                x.EventDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.EventType == "InvoiceIssued" ? "Invoice Issued" : "Payment Recorded",
                x.InvoiceNumber,
                x.InvoiceStatus ?? string.Empty,
                x.Currency,
                x.Amount.ToString("N2", CultureInfo.InvariantCulture),
                x.OutstandingAmount?.ToString("N2", CultureInfo.InvariantCulture) ?? string.Empty,
                x.DaysOverdue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                x.ReferenceNumber ?? string.Empty,
            ])
            .ToList();

        return new ReportExportDocument("Organization Billing History", metadataLines, columns, rows);
    }
}
