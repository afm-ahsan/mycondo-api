namespace MyCondo.Domain.Features.Payroll.AttendanceRecords;

public readonly record struct AttendanceRecordId(Guid Value)
{
    public static AttendanceRecordId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static AttendanceRecordId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new AttendanceRecordId(g)
            : throw new FormatException($"Invalid AttendanceRecordId: '{s}'");
}
