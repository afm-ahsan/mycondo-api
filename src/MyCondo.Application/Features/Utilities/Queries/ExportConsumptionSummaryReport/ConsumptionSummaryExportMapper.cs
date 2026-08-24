using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;

public static class ConsumptionSummaryExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<ConsumptionSummaryLineDto> lines,
        Guid? buildingId,
        string? utilityType,
        DateOnly fromDate,
        DateOnly toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Building", buildingId?.ToString() ?? "All buildings"),
            ("Utility Type", utilityType ?? "All"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Utility Type"),
            new("Total Consumption (Units)", IsNumeric: true),
            new("Reading Count", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.UtilityType,
                line.TotalConsumptionUnits.ToString("N2", CultureInfo.InvariantCulture),
                line.ReadingCount.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();

        return new ReportExportDocument("Consumption Summary", metadataLines, columns, rows);
    }
}
