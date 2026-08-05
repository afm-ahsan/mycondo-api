using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

public sealed class RequestAttendanceCorrectionCommandHandler(
    IAttendanceRecordRepository attendanceRecords,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<RequestAttendanceCorrectionCommandHandler> logger
) : IRequestHandler<RequestAttendanceCorrectionCommand>
{
    public async ValueTask<Unit> Handle(RequestAttendanceCorrectionCommand command, CancellationToken cancellationToken)
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

        record.RequestCorrection(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Correction requested for attendance record {AttendanceRecordId}, tenant {TenantId}", id, tenantId);

        return Unit.Value;
    }
}
