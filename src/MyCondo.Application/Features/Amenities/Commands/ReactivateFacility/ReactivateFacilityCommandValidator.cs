using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.ReactivateFacility;

public sealed class ReactivateFacilityCommandValidator : AbstractValidator<ReactivateFacilityCommand>
{
    public ReactivateFacilityCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
    }
}
