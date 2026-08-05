using FluentValidation;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

public sealed class RequestAttendanceCorrectionCommandValidator : AbstractValidator<RequestAttendanceCorrectionCommand>
{
    public RequestAttendanceCorrectionCommandValidator()
    {
        RuleFor(x => x.AttendanceRecordId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
    }
}
