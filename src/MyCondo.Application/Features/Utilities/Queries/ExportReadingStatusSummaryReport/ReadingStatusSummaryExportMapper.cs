using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

public static class ReadingStatusSummaryExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<ReadingStatusSummaryLineDto> lines,
        Guid? buildingId,
        string? utilityType)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Building", buildingId?.ToString() ?? "All buildings"),
            ("Utility Type", utilityType ?? "All"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Utility Type"),
            new("Status"),
            new("Count", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.UtilityType,
                line.Status,
                line.Count.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Readings", lines.Sum(l => l.Count).ToString(CultureInfo.InvariantCulture)),
        ];

        return new ReportExportDocument("Reading Status Summary", metadataLines, columns, rows, totals);
    }
}
