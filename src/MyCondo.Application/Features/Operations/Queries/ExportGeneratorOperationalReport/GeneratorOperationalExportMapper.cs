using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorOperationalReport;

public static class GeneratorOperationalExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<GeneratorOperationalReportLineDto> lines, DateOnly fromDate, DateOnly toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        List<ReportExportColumn> columns =
        [
            new("Generator"),
            new("Sessions", IsNumeric: true),
            new("Runtime (min)", IsNumeric: true),
            new("Fuel Consumed", IsNumeric: true),
            new("Fuel Received", IsNumeric: true),
            new("Fuel Cost", IsNumeric: true),
            new("Cost/Hour", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.GeneratorName,
                line.SessionCount.ToString(CultureInfo.InvariantCulture),
                line.TotalRuntimeMinutes.ToString(CultureInfo.InvariantCulture),
                FormatAmount(line.TotalFuelConsumed),
                FormatAmount(line.TotalFuelReceived),
                FormatAmount(line.TotalFuelCost),
                line.CostPerHour is decimal costPerHour ? FormatAmount(costPerHour) : string.Empty,
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Sessions", lines.Sum(l => l.SessionCount).ToString(CultureInfo.InvariantCulture)),
            ("Total Runtime (min)", lines.Sum(l => l.TotalRuntimeMinutes).ToString(CultureInfo.InvariantCulture)),
            ("Total Fuel Cost", FormatAmount(lines.Sum(l => l.TotalFuelCost))),
        ];

        return new ReportExportDocument("Generator Operational", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
