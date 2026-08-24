using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.ExportPoolDailyUsageReport;

/// <summary>Unlike every other Amenities report, <see cref="PoolDailyUsageReportDto"/> has no natural
/// row list — it is a single day's set of scalar counters for one pool. Rather than stretching the
/// Totals-only convention (which exists for report-wide summaries alongside a real table), this maps
/// each counter to its own Metric/Value row so CSV/PDF still render a genuine table.</summary>
public static class PoolDailyUsageExportMapper
{
    public static ReportExportDocument ToExportDocument(PoolDailyUsageReportDto report, string facilityName)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Date", report.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Facility", facilityName),
        ];

        List<ReportExportColumn> columns =
        [
            new("Metric"),
            new("Count", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows =
        [
            RowOf("Total Entries", report.TotalEntries),
            RowOf("Resident Entries", report.ResidentEntries),
            RowOf("Guest Entries", report.GuestEntries),
            RowOf("Adult Entries", report.AdultEntries),
            RowOf("Child Entries", report.ChildEntries),
            RowOf("Peak Concurrent Occupancy", report.PeakConcurrentOccupancy),
        ];

        return new ReportExportDocument("Pool Daily Usage Report", metadataLines, columns, rows);
    }

    private static IReadOnlyList<string> RowOf(string metric, int value) =>
        [metric, value.ToString(CultureInfo.InvariantCulture)];
}
