using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;

public sealed class StartGeneratorSessionCommandValidator : AbstractValidator<StartGeneratorSessionCommand>
{
    public StartGeneratorSessionCommandValidator()
    {
        RuleFor(x => x.GeneratorId).NotEmpty();
        RuleFor(x => x.OpeningFuelLevel).GreaterThanOrEqualTo(0);
    }
}
