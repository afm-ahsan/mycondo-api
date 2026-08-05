using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockIn;

public sealed class ClockInCommandHandler(
    IAttendanceRecordRepository attendanceRecords,
    IStaffMemberRepository staffMembers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ClockInCommandHandler> logger
) : IRequestHandler<ClockInCommand, AttendanceRecordDto>
{
    public async ValueTask<AttendanceRecordDto> Handle(ClockInCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        StaffMemberId staffMemberId = new(command.StaffMemberId);
        StaffMember staffMember = await staffMembers.GetByIdAsync(staffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), command.StaffMemberId);
        if (staffMember.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(StaffMember), command.StaffMemberId);
        }

        AttendanceRecord? open = await attendanceRecords.GetOpenRecordForStaffMemberAsync(tenantId, staffMemberId, cancellationToken);
        if (open is not null)
        {
            throw new ConflictException("This staff member already has an open (not clocked-out) attendance record.");
        }

        AttendanceSource source = Enum.Parse<AttendanceSource>(command.Source);

        AttendanceRecord record = AttendanceRecord.ClockIn(
            tenantId, staffMemberId, command.WorkDate, command.ScheduledStartUtc, command.ScheduledEndUtc,
            clock.UtcNow, command.WorkLocation, source);

        attendanceRecords.Add(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff member {StaffMemberId} clocked in via attendance record {AttendanceRecordId} for tenant {TenantId}",
            staffMemberId, record.Id, tenantId);

        return record.ToDto();
    }
}
