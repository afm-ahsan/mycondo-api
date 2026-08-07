using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Commands.AssignWorkerToOccupancyRegistration;

public sealed class AssignWorkerToOccupancyRegistrationCommandValidator
    : AbstractValidator<AssignWorkerToOccupancyRegistrationCommand>
{
    public AssignWorkerToOccupancyRegistrationCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.DomesticWorkerProfileId).NotEmpty();
    }
}
