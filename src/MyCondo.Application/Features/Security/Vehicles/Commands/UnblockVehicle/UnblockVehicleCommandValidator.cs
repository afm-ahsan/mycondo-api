using FluentValidation;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.UnblockVehicle;

public sealed class UnblockVehicleCommandValidator : AbstractValidator<UnblockVehicleCommand>
{
    public UnblockVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}
