using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByOwner;

public sealed class RequestOccupancyRegistrationCorrectionsByOwnerCommandValidator
    : AbstractValidator<RequestOccupancyRegistrationCorrectionsByOwnerCommand>
{
    public RequestOccupancyRegistrationCorrectionsByOwnerCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
