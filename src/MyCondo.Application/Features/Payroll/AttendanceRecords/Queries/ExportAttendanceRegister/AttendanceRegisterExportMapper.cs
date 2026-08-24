using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.ExportAttendanceRegister;

public static class AttendanceRegisterExportMapper
{
    public static ReportExportDocument ToExportDocument(
        IReadOnlyList<AttendanceRegisterEntryDto> entries, DateOnly? workDate, Guid? staffMemberId, bool? onlyOpen)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Work Date", workDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "All dates"),
            ("Staff Member", DescribeStaffMemberFilter(entries, staffMemberId)),
            ("Only Currently Open", onlyOpen == true ? "Yes" : "No"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Staff Member"),
            new("Role"),
            new("Work Date"),
            new("Scheduled Start"),
            new("Scheduled End"),
            new("Check In"),
            new("Check Out"),
            new("Overtime (min)", IsNumeric: true),
            new("Work Location"),
            new("Source"),
            new("Late Arrival"),
            new("Early Departure"),
            new("Correction Requested"),
            new("Correction Reason"),
            new("Approved At"),
        ];

        List<IReadOnlyList<string>> rows = entries
            .Select(e => (IReadOnlyList<string>)
            [
                e.StaffMemberFullName,
                e.StaffMemberRole,
                e.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                FormatDateTimeOffset(e.ScheduledStartUtc),
                FormatDateTimeOffset(e.ScheduledEndUtc),
                FormatDateTimeOffset(e.CheckInUtc),
                FormatDateTimeOffset(e.CheckOutUtc),
                e.OvertimeMinutes.ToString(CultureInfo.InvariantCulture),
                e.WorkLocation ?? string.Empty,
                e.Source,
                e.IsLateArrival ? "Yes" : "No",
                e.IsEarlyDeparture ? "Yes" : "No",
                e.CorrectionRequested ? "Yes" : "No",
                e.CorrectionReason ?? string.Empty,
                FormatDateTimeOffset(e.ApprovedAtUtc),
            ])
            .ToList();

        return new ReportExportDocument("Attendance Register", metadataLines, columns, rows);
    }

    private static string DescribeStaffMemberFilter(IReadOnlyList<AttendanceRegisterEntryDto> entries, Guid? staffMemberId)
    {
        if (staffMemberId is not Guid id)
        {
            return "All staff";
        }

        AttendanceRegisterEntryDto? match = entries.FirstOrDefault(e => e.StaffMemberId == id);
        return match?.StaffMemberFullName ?? id.ToString();
    }

    private static string FormatDateTimeOffset(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDateTimeOffset(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
