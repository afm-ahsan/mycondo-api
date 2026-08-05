using AwesomeAssertions;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.AttendanceRecords.Exceptions;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Domain.UnitTests.Features.Payroll.AttendanceRecords;

public class AttendanceRecordTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly StaffMemberId StaffMemberId = StaffMemberId.New();
    private static readonly DateOnly WorkDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void ClockIn_Starts_Open()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, "Main Gate", AttendanceSource.Manual);

        record.CheckOutUtc.Should().BeNull();
        record.CheckInUtc.Should().Be(Now);
    }

    [Fact]
    public void ClockOut_Sets_CheckOutUtc()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, null, AttendanceSource.Manual);

        record.ClockOut(Now.AddHours(8));

        record.CheckOutUtc.Should().Be(Now.AddHours(8));
    }

    [Fact]
    public void ClockOut_Throws_When_Already_Closed()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, null, AttendanceSource.Manual);
        record.ClockOut(Now.AddHours(8));

        Action act = () => record.ClockOut(Now.AddHours(9));

        act.Should().Throw<AttendanceRecordAlreadyClosedException>();
    }

    [Fact]
    public void IsLateArrival_True_When_CheckIn_After_ScheduledStart()
    {
        DateTimeOffset scheduledStart = Now;
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, scheduledStart, null, Now.AddMinutes(15), null, AttendanceSource.Manual);

        record.IsLateArrival.Should().BeTrue();
    }

    [Fact]
    public void IsLateArrival_False_When_No_ScheduledStart()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, null, AttendanceSource.Manual);

        record.IsLateArrival.Should().BeFalse();
    }

    [Fact]
    public void OvertimeMinutes_Computed_From_ScheduledEnd_Vs_CheckOut()
    {
        DateTimeOffset scheduledEnd = Now;
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, scheduledEnd, Now.AddHours(-8), null, AttendanceSource.Manual);

        record.ClockOut(scheduledEnd.AddMinutes(30));

        record.OvertimeMinutes.Should().Be(30);
        record.IsEarlyDeparture.Should().BeFalse();
    }

    [Fact]
    public void IsEarlyDeparture_True_When_CheckOut_Before_ScheduledEnd()
    {
        DateTimeOffset scheduledEnd = Now;
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, scheduledEnd, Now.AddHours(-8), null, AttendanceSource.Manual);

        record.ClockOut(scheduledEnd.AddMinutes(-30));

        record.IsEarlyDeparture.Should().BeTrue();
        record.OvertimeMinutes.Should().Be(0);
    }

    [Fact]
    public void RequestCorrection_Sets_Flag_And_Reason()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, null, AttendanceSource.Manual);

        record.RequestCorrection("Forgot to clock in on time");

        record.CorrectionRequested.Should().BeTrue();
        record.CorrectionReason.Should().Be("Forgot to clock in on time");
    }

    [Fact]
    public void ApproveCorrection_Clears_Requested_Flag_And_Sets_Approver()
    {
        AttendanceRecord record = AttendanceRecord.ClockIn(
            TenantId, StaffMemberId, WorkDate, null, null, Now, null, AttendanceSource.Manual);
        record.RequestCorrection("Reason");
        Guid approver = Guid.NewGuid();

        record.ApproveCorrection(approver, Now.AddHours(1));

        record.CorrectionRequested.Should().BeFalse();
        record.ApprovedBy.Should().Be(approver);
        record.ApprovedAtUtc.Should().Be(Now.AddHours(1));
    }
}
