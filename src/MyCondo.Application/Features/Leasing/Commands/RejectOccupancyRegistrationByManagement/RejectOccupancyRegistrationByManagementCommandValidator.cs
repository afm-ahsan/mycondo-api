using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByManagement;

public sealed class RejectOccupancyRegistrationByManagementCommandValidator
    : AbstractValidator<RejectOccupancyRegistrationByManagementCommand>
{
    public RejectOccupancyRegistrationByManagementCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
