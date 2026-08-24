using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.ExportFacilityUtilizationReport;

public static class FacilityUtilizationExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<FacilityUtilizationReportLineDto> lines,
        DateOnly fromDate,
        DateOnly toDate,
        string? facilityScope)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("Facility", facilityScope ?? "All Facilities"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Facility"),
            new("Total Bookings", IsNumeric: true),
            new("Completed", IsNumeric: true),
            new("Cancelled", IsNumeric: true),
            new("No-Show", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.FacilityName,
                line.TotalBookings.ToString(CultureInfo.InvariantCulture),
                line.CompletedBookings.ToString(CultureInfo.InvariantCulture),
                line.CancelledBookings.ToString(CultureInfo.InvariantCulture),
                line.NoShowBookings.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Bookings", lines.Sum(l => l.TotalBookings).ToString(CultureInfo.InvariantCulture)),
            ("Completed", lines.Sum(l => l.CompletedBookings).ToString(CultureInfo.InvariantCulture)),
            ("Cancelled", lines.Sum(l => l.CancelledBookings).ToString(CultureInfo.InvariantCulture)),
            ("No-Show", lines.Sum(l => l.NoShowBookings).ToString(CultureInfo.InvariantCulture)),
        ];

        return new ReportExportDocument("Facility Utilization Report", metadataLines, columns, rows, totals);
    }
}
