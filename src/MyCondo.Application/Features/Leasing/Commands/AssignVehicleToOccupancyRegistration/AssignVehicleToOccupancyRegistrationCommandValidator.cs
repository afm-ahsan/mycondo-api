using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.AssignVehicleToOccupancyRegistration;

public sealed class AssignVehicleToOccupancyRegistrationCommandValidator
    : AbstractValidator<AssignVehicleToOccupancyRegistrationCommand>
{
    public AssignVehicleToOccupancyRegistrationCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}
