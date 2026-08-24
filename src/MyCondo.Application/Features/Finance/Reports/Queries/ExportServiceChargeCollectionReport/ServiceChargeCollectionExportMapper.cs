using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;

public static class ServiceChargeCollectionExportMapper
{
    public static ReportExportDocument ToExportDocument(ServiceChargeCollectionReportDto report)
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
            new("Value", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            ["Billed", FormatAmount(report.Billed)],
            ["Billed Invoice Count", report.BilledInvoiceCount.ToString(CultureInfo.InvariantCulture)],
            ["Collected", FormatAmount(report.Collected)],
            ["Waived", FormatAmount(report.Waived)],
        ];

        return new ReportExportDocument("Service Charge Collection", metadataLines, columns, rows);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
