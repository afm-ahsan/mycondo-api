using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.CompleteMaintenanceService;

public sealed class CompleteMaintenanceServiceCommandValidator : AbstractValidator<CompleteMaintenanceServiceCommand>
{
    public CompleteMaintenanceServiceCommandValidator()
    {
        RuleFor(x => x.GeneratorMaintenanceScheduleId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost is not null);
        RuleFor(x => x.NextDueHourMeterReading).GreaterThanOrEqualTo(0).When(x => x.NextDueHourMeterReading is not null);
        RuleFor(x => x)
            .Must(x => x.NextDueDate is not null || x.NextDueHourMeterReading is not null)
            .WithMessage("At least one of NextDueDate or NextDueHourMeterReading is required.");
    }
}
