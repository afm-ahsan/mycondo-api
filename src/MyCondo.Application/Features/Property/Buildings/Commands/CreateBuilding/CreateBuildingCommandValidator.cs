using FluentValidation;

namespace MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;

public sealed class CreateBuildingCommandValidator : AbstractValidator<CreateBuildingCommand>
{
    public CreateBuildingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(400);
    }
}
