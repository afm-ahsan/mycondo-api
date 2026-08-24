using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.ExportSupplierComparisonReport;

public static class SupplierComparisonExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<SupplierComparisonReportLineDto> lines, DateOnly fromDate, DateOnly toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        List<ReportExportColumn> columns =
        [
            new("Supplier"),
            new("Purchases", IsNumeric: true),
            new("Total Quantity (kg)", IsNumeric: true),
            new("Total Amount", IsNumeric: true),
            new("Avg Unit Price/kg", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.SupplierName,
                line.PurchaseCount.ToString(CultureInfo.InvariantCulture),
                line.TotalQuantity.ToString(CultureInfo.InvariantCulture),
                FormatAmount(line.TotalAmount),
                FormatAmount(line.AverageUnitPricePerKg),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Quantity (kg)", lines.Sum(l => l.TotalQuantity).ToString(CultureInfo.InvariantCulture)),
            ("Total Amount", FormatAmount(lines.Sum(l => l.TotalAmount))),
        ];

        return new ReportExportDocument("Supplier Comparison", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
