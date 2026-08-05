using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ApproveAttendanceCorrection;

public sealed class ApproveAttendanceCorrectionCommandHandler(
    IAttendanceRecordRepository attendanceRecords,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ApproveAttendanceCorrectionCommandHandler> logger
) : IRequestHandler<ApproveAttendanceCorrectionCommand>
{
    public async ValueTask<Unit> Handle(ApproveAttendanceCorrectionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        Guid approvedBy = currentUser.UserId ?? throw new ForbiddenException("Authentication required.");

        AttendanceRecordId id = new(command.AttendanceRecordId);
        AttendanceRecord record = await attendanceRecords.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AttendanceRecord), command.AttendanceRecordId);

        if (record.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(AttendanceRecord), command.AttendanceRecordId);
        }

        record.ApproveCorrection(approvedBy, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Correction approved for attendance record {AttendanceRecordId} by {ApprovedBy}, tenant {TenantId}",
            id, approvedBy, tenantId);

        return Unit.Value;
    }
}
