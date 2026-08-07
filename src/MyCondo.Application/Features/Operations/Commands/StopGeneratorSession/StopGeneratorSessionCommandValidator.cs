using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.StopGeneratorSession;

public sealed class StopGeneratorSessionCommandValidator : AbstractValidator<StopGeneratorSessionCommand>
{
    public StopGeneratorSessionCommandValidator()
    {
        RuleFor(x => x.GeneratorSessionId).NotEmpty();
        RuleFor(x => x.ClosingFuelLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OutageReason).MaximumLength(500);
        RuleFor(x => x.HourMeterReading).GreaterThanOrEqualTo(0).When(x => x.HourMeterReading is not null);
    }
}
