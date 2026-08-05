using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInVehicle;

public sealed class CheckInVehicleCommandValidator : AbstractValidator<CheckInVehicleCommand>
{
    public CheckInVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.EntryGateId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.OverrideReason).MaximumLength(400);
    }
}
