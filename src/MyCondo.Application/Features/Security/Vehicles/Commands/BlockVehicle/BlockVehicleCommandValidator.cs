using FluentValidation;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.BlockVehicle;

public sealed class BlockVehicleCommandValidator : AbstractValidator<BlockVehicleCommand>
{
    public BlockVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
    }
}
