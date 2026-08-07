using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByManagement;

public sealed class RequestOccupancyRegistrationCorrectionsByManagementCommandValidator
    : AbstractValidator<RequestOccupancyRegistrationCorrectionsByManagementCommand>
{
    public RequestOccupancyRegistrationCorrectionsByManagementCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
