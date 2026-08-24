using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorMaintenanceDueReport;

public static class GeneratorMaintenanceDueExportMapper
{
    public static ReportExportDocument ToExportDocument(IReadOnlyList<GeneratorMaintenanceDueReportLineDto> lines)
    {
        List<ReportExportColumn> columns =
        [
            new("Generator"),
            new("Next Due Date"),
            new("Next Due Hour Meter Reading", IsNumeric: true),
            new("Current Hour Meter Reading", IsNumeric: true),
            new("Due"),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.GeneratorName,
                line.NextDueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                line.NextDueHourMeterReading is decimal nextDueReading ? FormatAmount(nextDueReading) : string.Empty,
                FormatAmount(line.CurrentHourMeterReading),
                line.IsDue ? "Yes" : "No",
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Due Count", lines.Count(l => l.IsDue).ToString(CultureInfo.InvariantCulture)),
        ];

        return new ReportExportDocument("Generator Maintenance Due", [], columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
