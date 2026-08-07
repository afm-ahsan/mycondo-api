using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;

public sealed class MoveOutOccupancyRegistrationCommandValidator : AbstractValidator<MoveOutOccupancyRegistrationCommand>
{
    public MoveOutOccupancyRegistrationCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
