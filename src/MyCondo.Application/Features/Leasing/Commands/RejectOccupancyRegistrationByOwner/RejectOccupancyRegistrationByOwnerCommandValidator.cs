using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByOwner;

public sealed class RejectOccupancyRegistrationByOwnerCommandValidator
    : AbstractValidator<RejectOccupancyRegistrationByOwnerCommand>
{
    public RejectOccupancyRegistrationByOwnerCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
