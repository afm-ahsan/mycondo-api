using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.DeactivateFacility;

public sealed class DeactivateFacilityCommandValidator : AbstractValidator<DeactivateFacilityCommand>
{
    public DeactivateFacilityCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
    }
}
