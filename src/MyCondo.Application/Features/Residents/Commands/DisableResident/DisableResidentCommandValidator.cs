using FluentValidation;

namespace MyCondo.Application.Features.Residents.Commands.DisableResident;

public sealed class DisableResidentCommandValidator : AbstractValidator<DisableResidentCommand>
{
    public DisableResidentCommandValidator()
    {
        RuleFor(x => x.ResidentId).NotEmpty();
    }
}
