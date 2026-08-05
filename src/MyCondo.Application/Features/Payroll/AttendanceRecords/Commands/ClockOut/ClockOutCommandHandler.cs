using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.AttendanceRecords.DTOs;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockOut;

public sealed class ClockOutCommandHandler(
    IAttendanceRecordRepository attendanceRecords,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ClockOutCommandHandler> logger
) : IRequestHandler<ClockOutCommand, AttendanceRecordDto>
{
    public async ValueTask<AttendanceRecordDto> Handle(ClockOutCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AttendanceRecordId id = new(command.AttendanceRecordId);
        AttendanceRecord record = await attendanceRecords.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AttendanceRecord), command.AttendanceRecordId);

        if (record.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(AttendanceRecord), command.AttendanceRecordId);
        }

        record.ClockOut(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Attendance record {AttendanceRecordId} clocked out for tenant {TenantId}", id, tenantId);

        return record.ToDto();
    }
}
