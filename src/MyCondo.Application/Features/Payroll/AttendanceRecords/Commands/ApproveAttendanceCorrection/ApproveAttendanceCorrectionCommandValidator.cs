using FluentValidation;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ApproveAttendanceCorrection;

public sealed class ApproveAttendanceCorrectionCommandValidator : AbstractValidator<ApproveAttendanceCorrectionCommand>
{
    public ApproveAttendanceCorrectionCommandValidator()
    {
        RuleFor(x => x.AttendanceRecordId).NotEmpty();
    }
}
