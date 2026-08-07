using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.CreateMaintenanceSchedule;

public sealed class CreateMaintenanceScheduleCommandValidator : AbstractValidator<CreateMaintenanceScheduleCommand>
{
    public CreateMaintenanceScheduleCommandValidator()
    {
        RuleFor(x => x.GeneratorId).NotEmpty();
        RuleFor(x => x.NextDueHourMeterReading).GreaterThanOrEqualTo(0).When(x => x.NextDueHourMeterReading is not null);
        RuleFor(x => x)
            .Must(x => x.NextDueDate is not null || x.NextDueHourMeterReading is not null)
            .WithMessage("At least one of NextDueDate or NextDueHourMeterReading is required.");
    }
}
