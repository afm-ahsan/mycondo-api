using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.UpdateMaintenanceSchedule;

public sealed class UpdateMaintenanceScheduleCommandValidator : AbstractValidator<UpdateMaintenanceScheduleCommand>
{
    public UpdateMaintenanceScheduleCommandValidator()
    {
        RuleFor(x => x.GeneratorMaintenanceScheduleId).NotEmpty();
        RuleFor(x => x.NextDueHourMeterReading).GreaterThanOrEqualTo(0).When(x => x.NextDueHourMeterReading is not null);
        RuleFor(x => x)
            .Must(x => x.NextDueDate is not null || x.NextDueHourMeterReading is not null)
            .WithMessage("At least one of NextDueDate or NextDueHourMeterReading is required.");
    }
}
