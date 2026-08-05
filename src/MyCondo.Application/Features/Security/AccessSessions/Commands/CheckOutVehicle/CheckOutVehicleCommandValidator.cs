using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutVehicle;

public sealed class CheckOutVehicleCommandValidator : AbstractValidator<CheckOutVehicleCommand>
{
    public CheckOutVehicleCommandValidator()
    {
        RuleFor(x => x.AccessSessionId).NotEmpty();
        RuleFor(x => x.ExitGateId).NotEmpty();
    }
}
