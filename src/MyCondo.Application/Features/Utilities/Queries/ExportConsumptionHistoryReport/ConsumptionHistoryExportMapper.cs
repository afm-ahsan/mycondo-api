using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;

public static class ConsumptionHistoryExportMapper
{
    public static ReportExportDocument ToExportDocument(IReadOnlyList<ReadingDto> readings, Meter meter, Building? building)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Building", building?.Name ?? meter.BuildingId.Value.ToString()),
            ("Meter", meter.MeterNumber),
            ("Utility Type", meter.UtilityType.ToString()),
        ];

        List<ReportExportColumn> columns =
        [
            new("Period Start"),
            new("Period End"),
            new("Consumption (Units)", IsNumeric: true),
            new("Status"),
            new("Abnormal"),
        ];

        List<IReadOnlyList<string>> rows = readings
            .OrderBy(r => r.PeriodStart)
            .Select(r => (IReadOnlyList<string>)
            [
                r.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.ConsumptionUnits.ToString("N2", CultureInfo.InvariantCulture),
                r.Status,
                r.IsAbnormalConsumption ? "Yes" : "No",
            ])
            .ToList();

        return new ReportExportDocument($"{meter.UtilityType} Consumption History", metadataLines, columns, rows);
    }
}
