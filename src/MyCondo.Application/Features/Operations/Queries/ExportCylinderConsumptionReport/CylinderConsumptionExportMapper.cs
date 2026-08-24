using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.ExportCylinderConsumptionReport;

public static class CylinderConsumptionExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<CylinderConsumptionReportLineDto> lines, string? cylinderType, DateOnly fromDate, DateOnly toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Cylinder Type", string.IsNullOrWhiteSpace(cylinderType) ? "All" : cylinderType),
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        List<ReportExportColumn> columns =
        [
            new("Cylinder Type"),
            new("Total Received", IsNumeric: true),
            new("Total Issued", IsNumeric: true),
            new("Total Empty Returned", IsNumeric: true),
            new("Net Change", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.CylinderType,
                line.TotalReceived.ToString(CultureInfo.InvariantCulture),
                line.TotalIssued.ToString(CultureInfo.InvariantCulture),
                line.TotalEmptyReturned.ToString(CultureInfo.InvariantCulture),
                line.NetChange.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Received", lines.Sum(l => l.TotalReceived).ToString(CultureInfo.InvariantCulture)),
            ("Total Issued", lines.Sum(l => l.TotalIssued).ToString(CultureInfo.InvariantCulture)),
            ("Total Empty Returned", lines.Sum(l => l.TotalEmptyReturned).ToString(CultureInfo.InvariantCulture)),
            ("Net Change", lines.Sum(l => l.NetChange).ToString(CultureInfo.InvariantCulture)),
        ];

        return new ReportExportDocument("Cylinder Consumption", metadataLines, columns, rows, totals);
    }
}
