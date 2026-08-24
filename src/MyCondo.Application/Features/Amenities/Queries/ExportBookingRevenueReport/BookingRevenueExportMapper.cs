using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.ExportBookingRevenueReport;

public static class BookingRevenueExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<BookingRevenueReportLineDto> lines, DateOnly fromDate, DateOnly toDate)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("To", toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        List<ReportExportColumn> columns =
        [
            new("Facility"),
            new("Billable Bookings", IsNumeric: true),
            new("Booking Charges", IsNumeric: true),
            new("Deposits Forfeited", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.FacilityName,
                line.BillableBookingCount.ToString(CultureInfo.InvariantCulture),
                FormatAmount(line.TotalBookingCharges),
                FormatAmount(line.TotalDepositsForfeited),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Billable Bookings", lines.Sum(l => l.BillableBookingCount).ToString(CultureInfo.InvariantCulture)),
            ("Booking Charges", FormatAmount(lines.Sum(l => l.TotalBookingCharges))),
            ("Deposits Forfeited", FormatAmount(lines.Sum(l => l.TotalDepositsForfeited))),
        ];

        return new ReportExportDocument("Booking Revenue Report", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
