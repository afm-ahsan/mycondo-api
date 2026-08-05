using FluentValidation;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockOut;

public sealed class ClockOutCommandValidator : AbstractValidator<ClockOutCommand>
{
    public ClockOutCommandValidator()
    {
        RuleFor(x => x.AttendanceRecordId).NotEmpty();
    }
}
