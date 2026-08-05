using FluentValidation;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockIn;

public sealed class ClockInCommandValidator : AbstractValidator<ClockInCommand>
{
    public ClockInCommandValidator()
    {
        RuleFor(x => x.StaffMemberId).NotEmpty();
        RuleFor(x => x.WorkLocation).MaximumLength(200);
        RuleFor(x => x.ScheduledEndUtc).GreaterThan(x => x.ScheduledStartUtc)
            .When(x => x.ScheduledStartUtc is not null && x.ScheduledEndUtc is not null);
        RuleFor(x => x.Source).Must(BeAValidSource)
            .WithMessage($"Source must be one of: {string.Join(", ", Enum.GetNames<AttendanceSource>())}.");
    }

    private static bool BeAValidSource(string value) => Enum.TryParse<AttendanceSource>(value, out _);
}
