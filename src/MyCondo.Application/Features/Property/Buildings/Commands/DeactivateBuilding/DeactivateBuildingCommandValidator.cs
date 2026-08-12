using FluentValidation;

namespace MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;

public sealed class DeactivateBuildingCommandValidator : AbstractValidator<DeactivateBuildingCommand>
{
    public DeactivateBuildingCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
    }
}
